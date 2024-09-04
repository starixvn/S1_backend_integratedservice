using MySql.Data.MySqlClient;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class PG2BLL : IPG2BLL
{
    #region Fields
    public GameType GameType { get; set; }
    private readonly Tp thisTp;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly AuthBLL _authBll;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly ILogger<PG2BLL> _logger;
    private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
    private readonly MySqlClient _mainDbClient;

    #endregion

    #region Constructors

    public PG2BLL(IServiceProvider provider)
    {
        thisTp = Tp.PG2;
        GameType = GameType.PG2_RNG;
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _authBll = provider.GetRequiredService<AuthBLL>();
        _bettingService = provider.GetService<BettingService>();
        _logger = provider.GetRequiredService<ILogger<PG2BLL>>();
        _settledService = provider.GetRequiredService<SettledService>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(thisTp.ToString());
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    #endregion

    public Response_PG2 GetBalance(CommonRequest_PG2 request)
    {
        var message = string.Empty;

        if (!CheckRequestForGetBalance(request, out message))
        {
            return new Response_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.InvalidRequest, message = message }
            };
        }

        var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, request.UserName, (int)GameType);
        if (tpCustBalance == null)
        {
            _logger.LogError($"GetBalance Fail: balance not found.");
            return new Response_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }


        if (!CheckAgentId(request.Agent,request.UserName))
        {
            return new Response_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.InvalidAgent, message = message }
            };
        }

        if (!CheckThirdPartyForGetBalance(request.UserName, out message))
        {
            return new Response_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        return new Response_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BalanceData_PG2() { balance = tpCustBalance.Balance }
        };
    }

    public async Task<BetResponse_PG2> Bet(BetRequest_PG2 request)
    {
        var message = string.Empty;
        decimal beforeBalance = 0; decimal afterBalance = 0;

        if (!CheckRequestForSlotBet(request, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.InvalidRequest, message = message }
            };
        }

        if (!CheckThirdPartyForBet(request.UserName, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.InvalidRequest, message = message }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.InvalidAgent, message = message }
            };
        }

        if (!GetBalanceForBet(request.UserName, out beforeBalance, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!CheckBeforeBalanceForBet(request.Amount, beforeBalance, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.BalanceInsufficient, message = message }
            };
        }

        if (!GetTpCustInfoForBet(request.UserName, out TpCustInfo tpCustInfo, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!GetTpCurrencyForBet(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        var partitionTime = DateTimeOffset.Parse(request.Timestamp).DateTime;

        var data = new BettingDataRNG_PG2()
        {
            username = request.UserName,
            agent = request.Agent,
            type = request.Type,
            amount = Math.Abs(request.Amount) * -1,  //Bet為減項
            product = request.Product,
            category = request.Category,
            gameName = request.GameName,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = partitionTime,
            isFeatureBuy = request.IsFeatureBuy,
            FromAPIType = BettransRng_PG2TypeEnum.FromBet,
            TicketType = BettransType.Bet,
            TicketStatus = TicketStatus.UnSettled
        };

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!CheckBetForBet(partitionTime, data, out message))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.DuplicateRound, message = message }
            };
        }

        if (!PlaceBetForBet(partitionTime, data, out message, trans))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (data.amount != 0)
        {
            if (!SetBalanceForBet(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new BetResponse_PG2()
                {
                    status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
                };
            }
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        return new BetResponse_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BetData_PG2()
            {
                username = request.UserName,
                wallet = new walletmodel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                balance = new balancemodel() { before = beforeBalance, after = afterBalance },
                refId = request.RefId
            }
            
        };
    }

    public Task<Response_PG2> Payout(PayoutRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    public Task<Response_PG2> Settlement(SettlementRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    public Task<Response_PG2> Cancel(CancelRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    #region private
    #region getbalance
    private bool CheckRequestForGetBalance(CommonRequest_PG2 request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Agent))
        {
            message = "request.agent is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            message = "request.username is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }
    private bool CheckThirdPartyForGetBalance(string username, out string message)
    {
        message = string.Empty;
        try
        {
            var result = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, username);

            if (result == null)
            {
                message = "player not found.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            if (result.Locked)
            {
                message = "player is locked.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
            return false;
        }
    }
    #endregion
    #region bet
    private bool CheckRequestForSlotBet(BetRequest_PG2 request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            message = "request.UserName is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Agent))
        {
            message = "request.agent is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            message = "request.Type is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Product))
        {
            message = "request.Product is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            message = "request.Category is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.GameName))
        {
            message = "request.game is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RoundId))
        {
            message = "request.roundId is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RefId))
        {
            message = "request.RefId is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Timestamp))
        {
            message = "request.timestamp is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }
    private bool CheckThirdPartyForBet(string username, out string message)
    {
        message = string.Empty;
        try
        {
            var result = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, username);

            if (result == null)
            {
                message = "player not found.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            if (result.Locked)
            {
                message = "player is Locked.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
            return false;
        }
    }
    private bool GetBalanceForBet(string username, out decimal beforeBalance, out string message)
    {
        message = string.Empty;
        beforeBalance = 0;
        try
        {
            var result = _tpCustInfoBll.GetBalance((int)thisTp, username, (int)GameType);

            if (result == null)
            {
                message = "balance not found.";
                _logger.LogError($"GetBalance Fail:{message}");
                return false;
            }

            beforeBalance = result.Balance;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetBalance Exception:{ex.ToString()}");
            return false;
        }
    }
    private bool CheckBeforeBalanceForBet(decimal betAmount, decimal beforeBalance, out string message)
    {
        message = string.Empty;

        if (beforeBalance - betAmount < 0)
        {
            message = "Balance insufficient.";
            _logger.LogError($"CheckBalance Fail:{message}");
            return false;
        }

        return true;
    }
    private bool GetTpCustInfoForBet(string username, out TpCustInfo tpCustInfo, out string message)
    {
        message = string.Empty;
        tpCustInfo = null;
        try
        {
            tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp.PG2, username);
            if (tpCustInfo == null)
            {
                message = "tpCustInfo is null.";
                _logger.LogError($"GetTpCustInfo Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.ToString()}");
            return false;
        }
    }

    private bool GetTpCurrencyForBet(int currencyId, out TpCurrency tpCurrency, out string message)
    {
        message = string.Empty;
        tpCurrency = null;
        try
        {
            tpCurrency = _thirdPartyBll.GetTpCurrency((int)thisTp, currencyId);
            if (tpCurrency == null)
            {
                message = "tpCurrency is null.";
                _logger.LogError($"GetTpCurrency Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetTpCurrency Exception:{ex.ToString()}");
            return false;
        }
    }

    private bool CheckBetForBet(DateTime partitionTime, BettingDataRNG_PG2 data, out string message)
    {
        message = string.Empty;
        try
        {
            var slotDatas = _bettingService.IsExist_PG2(false, partitionTime, data);

            if (slotDatas.Any())
            {
                //已存在指定有效投注
                message = "already exist valid bet by transactionId and roundId.";
                _logger.LogError($"CheckBet Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckBet Exception:{ex.ToString()}");
            return false;
        }
    }
    private bool PlaceBetForBet(DateTime partitionTime, BettingDataRNG_PG2 data, out string message, MySqlTransaction trans)
    {
        message = string.Empty;
        try
        {
            var result = _bettingService.PlaceBet_PG2(partitionTime, data, trans);

            if (!result)
            {
                message = "place bet fail.";
                _logger.LogError($"PlaceBet Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"PlaceBet Exception:{ex.ToString()}");
            return false;
        }
    }
    private bool SetBalanceForBet(TpCustInfo tpCustInfo, BettingDataRNG_PG2 data, decimal rate,
        out string message, out decimal afterAmount, out int bonusId,
        MySqlConnection conn, MySqlTransaction trans)
    {
        message = string.Empty;
        afterAmount = 0;
        bonusId = 0;
        try
        {
            var result = _bettingService.SetBalance_PG2(tpCustInfo, data, out afterAmount, ref bonusId, conn, trans);

            if (!result)
            {
                message = "set balance fail.";
                _logger.LogError($"SetBalance Fail:{message}");
                return false;
            }
            afterAmount = (rate > 0) ? afterAmount / rate : afterAmount;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"SetBalance Exception:{ex.ToString()}");
            return false;
        }
    }
    #endregion
    private bool CheckAgentId(string agentId, string username)
    {
        try
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp.PG2, username);
            var Agent = _siteConfigDic[tpCustInfo.SiteId]["agent"];

            if (string.IsNullOrWhiteSpace(Agent))
            {
                _logger.LogError("CheckAgentId Fail: SiteConfig.agent is null.");
                return false;
            }

            if (Agent != agentId)
            {
                _logger.LogError($"CheckAgentId Fail: {Agent} is not equal {agentId}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CheckAgentId Exception:{ex.Message}");
            return false;
        }
    }

    private string GetCurrentGameTime()
    {
        var currentAt = new DateTimeOffset(DateTime.Now);
        return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
    }
    #endregion

}