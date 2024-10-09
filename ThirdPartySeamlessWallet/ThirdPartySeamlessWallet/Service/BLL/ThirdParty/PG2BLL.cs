using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class PG2BLL : IPG2BLL
{
    #region Fields

    public GameType _gameType { get; set; }
    private readonly Tp _thisTp;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly ILogger<PG2BLL> _logger;
    private readonly MySqlClient _mainDbClient;

    #endregion

    #region Constructors

    public PG2BLL(IServiceProvider provider)
    {
        _thisTp = Tp.PG2;
        _gameType = GameType.PG2_RNG;
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _bettingService = provider.GetService<BettingService>();
        _logger = provider.GetRequiredService<ILogger<PG2BLL>>();
        _settledService = provider.GetRequiredService<SettledService>();
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    #endregion

    public BalanceResponse_PG2 GetBalance(CommonRequest_PG2 request)
    {
        var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)_thisTp, request.UserName, (int)_gameType);
        if (tpCustBalance == null)
        {
            string message = "GetBalance Fail: balance not found.";
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.ServiceNotAvailable,
                    message = message
                }
            };
        }

        return new BalanceResponse_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BalanceData_PG2() { balance = tpCustBalance.Balance }
        };
    }

    public async Task<BetResponse_PG2> Bet(BetRequest_PG2 request)
    {
        decimal afterBalance = 0;
        string message;
        decimal beforeBalance;

        if (!GetBalance(request.UserName, out beforeBalance, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!CheckBeforeBalance(request.Amount, beforeBalance, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.BalanceInsufficient, message = message }
            };
        }

        if (!GetTpCustInfo(request.UserName, out TpCustInfo tpCustInfo, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
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
        if (tpCurrency.ExchangeRate != 0)
        {
            data.amount *= tpCurrency.ExchangeRate;
        }

        if (!CheckBet(partitionTime, data, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.DuplicateRound, message = message }
            };
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!InsertBettrans(BettransType.Bet, partitionTime, data, out message, trans))
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
                wallet = new Walletmodel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                balance = new Balancemodel() { before = beforeBalance, after = afterBalance },
                refId = request.RefId
            }
        };
    }

    public async Task<PayoutResponse_PG2> Payout(PayoutRequest_PG2 request)
    {
        string message;
        decimal beforeBalance;
        decimal afterBalance;

        if (!GetTpCustInfo(request.UserName, out TpCustInfo tpCustInfo, out message))
        {
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
        {
            return new PayoutResponse_PG2()
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
            amount = Math.Abs(request.Amount),
            product = request.Product,
            category = request.Category,
            gameName = request.GameName,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = partitionTime,
            isFeatureBuy = request.IsFeatureBuy,
            isFreeSpin = request.IsFreespin,
            isOnlyPayout = request.IsOnlyPayout,
            FromAPIType = BettransRng_PG2TypeEnum.FromPayout,
            TicketType = BettransType.Payout,
            TicketStatus = TicketStatus.UnSettled
        };
        if (tpCurrency.ExchangeRate != 0)
        {
            data.amount *= tpCurrency.ExchangeRate;
        }

        if (!CheckPayout(partitionTime, data, out message))
        {
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.DuplicateRound, message = message }
            };
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!InsertBettrans(BettransType.Payout, partitionTime, data, out message, trans))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (data.amount != 0)
        {
            if (!SetBalanceForPayout(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new PayoutResponse_PG2()
                {
                    status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
                };
            }
            //win => 有派彩，beforeBalance用afterBalance反推，此afterBalance已由SetBalanceForPayout處理換回匯率
            beforeBalance = afterBalance - request.Amount;
        }
        else
        {
            //lost => 沒有派彩，balance取db資料
            if (!GetBalance(request.UserName, out beforeBalance, out message))
            {
                return new PayoutResponse_PG2()
                {
                    status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
                };
            }
            afterBalance = beforeBalance;
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        return new PayoutResponse_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BetData_PG2()
            {
                username = request.UserName,
                wallet = new Walletmodel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                balance = new Balancemodel() { before = beforeBalance, after = afterBalance },
                refId = request.RefId
            }
        };
    }

    public async Task<SettleResponse_PG2> Settlement(SettlementRequest_PG2 request)
    {
        var bonusId = 0;
        var afterAmount = 0m;
        decimal beforeBalance;
        string message;

        if (!GetBalance(request.UserName, out beforeBalance, out message))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!CheckBeforeBalance(request.BetAmount, beforeBalance, out message))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.BalanceInsufficient, message = message }
            };
        }

        if (!GetTpCustInfo(request.UserName, out TpCustInfo tpCustInfo, out message))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (tpCurrency.ExchangeRate != 0)
        {
            request.Amount *= tpCurrency.ExchangeRate;
            request.BetAmount *= tpCurrency.ExchangeRate;
        }

        var partitionTime = DateTimeOffset.Parse(request.Timestamp).DateTime;

        var data = new BettingDataRNG_PG2()
        {
            username = request.UserName,
            agent = request.Agent,
            type = request.Type,
            amount = request.Amount, //winlose
            product = request.Product,
            gameName = request.GameName,
            category = request.Category,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = partitionTime,
            isFeatureBuy = request.IsFeatureBuy,
            betAmount = Math.Abs(request.BetAmount),
            isEndRound = request.IsEndRound,
            parentRoundId = request.ParentRoundId,
            isOnlyPayout = request.IsOnlyPayout,
            FromAPIType = BettransRng_PG2TypeEnum.FromPayout,
            TicketType = request.IsEndRound ? BettransType.EndRound : BettransType.Payout,
            TicketStatus = TicketStatus.UnSettled
        };

        var slotDatas = _bettingService.IsExist_PG2(false, partitionTime, data);
        if (slotDatas.Any())
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.DuplicateRoundId,
                    message = PG2ErrorCode.DuplicateRoundId.GetEnumDescription()
                }
            };
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        var result = _settledService.InsertBettrans_BetAndPayout_PG2(partitionTime, data, trans);
        if (!result)
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            _logger.LogError($"Settlement BetAndPayout failed :{JsonConvert.SerializeObject(data)}");
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.ServiceNotAvailable,
                    message = PG2ErrorCode.ServiceNotAvailable.GetEnumDescription()
                }
            };
        }

        if (data.betAmount != 0)
        {
            result = _bettingService.SetBalance_PG2(tpCustInfo, data, out afterAmount, ref bonusId, conn, trans);
            if (!result)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                _logger.LogError("Settlement - PlaceBet failed");
                return new SettleResponse_PG2()
                {
                    status = new Status_PG2()
                    {
                        code = PG2ErrorCode.BalanceInsufficient,
                        message = PG2ErrorCode.BalanceInsufficient.GetEnumDescription()
                    }
                };
            }
        }

        if (data.amount != 0)
        {
            result = _settledService.SetBalance_PG2(tpCustInfo, data, TransType.Payout, "Payout", out afterAmount, ref bonusId, conn, trans);
            if (!result)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                _logger.LogError("Settlement - Payout failed");
                return new SettleResponse_PG2()
                {
                    status = new Status_PG2()
                    {
                        code = PG2ErrorCode.ServiceNotAvailable,
                        message = PG2ErrorCode.ServiceNotAvailable.GetEnumDescription()
                    }
                };
            }
        }

        if (data.isEndRound)
        {
            result = _settledService.InsertUnsettle_PG2(partitionTime, data.parentRoundId, trans);
            if (!result)
            {
                _logger.LogError($"Settlement EndRound failed {data.roundId}");
                return new SettleResponse_PG2()
                {
                    status = new Status_PG2()
                    {
                        code = PG2ErrorCode.ServiceNotAvailable,
                        message = PG2ErrorCode.ServiceNotAvailable.GetEnumDescription()
                    }
                };
            }
        }

        if (afterAmount == 0)
        {
            var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, request.UserName, (int)_gameType);
            afterAmount = tpCustBalance.Balance;
        }
        else
        {
            if (tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        if (bonusId > 0)
        {
            _logger.LogInformation($"user {tpCustInfo.TpUserName} has bonus wallet bonusId:{bonusId} transId:{data.roundId}");
            _settledService.UpdateTpBonusFlag((int)_thisTp, data.roundId, (int)GameType.PG2_RNG, TpCategory.Slots, out var custId, out bonusId);
        }

        return new SettleResponse_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BetData_PG2()
            {
                username = request.UserName,
                wallet = new Walletmodel() { balance = afterAmount, lastUpdate = GetCurrentGameTime() },
                balance = new Balancemodel() { before = beforeBalance, after = afterAmount },
                refId = request.RefId
            }
        };
    }

    public async Task<CancelResponse_PG2> Cancel(CancelRequest_PG2 request)
    {
        decimal afterBalance = 0;
        string message;

        if (!GetTpCustInfo(request.UserName, out TpCustInfo tpCustInfo, out message))
        {
            return new CancelResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
        {
            return new CancelResponse_PG2()
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
            amount = Math.Abs(request.Amount),
            product = request.Product,
            category = request.Category,
            gameName = request.GameName,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = partitionTime,
            FromAPIType = BettransRng_PG2TypeEnum.FromPayout,
            TicketType = BettransType.CancelBet,
            TicketStatus = TicketStatus.UnSettled
        };

        if (tpCurrency.ExchangeRate != 0)
        {
            data.amount *= tpCurrency.ExchangeRate;
        }

        if (!CheckCancel(partitionTime, data, out int sq, out message))
        {
            return new CancelResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.DuplicateRound, message = message }
            };
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!UpdateBettrans(partitionTime, sq, out message, trans))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new CancelResponse_PG2()
            {
                status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
            };
        }

        if (data.amount != 0)
        {
            if (!SetBalanceForCancel(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new CancelResponse_PG2()
                {
                    status = new Status_PG2() { code = PG2ErrorCode.ServiceNotAvailable, message = message }
                };
            }
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        return new CancelResponse_PG2()
        {
            status = new Status_PG2() { code = PG2ErrorCode.Success, message = "Success" },
            data = new BetData_PG2()
            {
                username = request.UserName,
                wallet = new Walletmodel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                balance = new Balancemodel() { before = afterBalance - request.Amount, after = afterBalance },
                refId = request.RefId
            }
        };
    }

    #region bet

    private bool CheckBet(DateTime partitionTime, BettingDataRNG_PG2 data, out string message)
    {
        message = string.Empty;
        try
        {
            var slotDatas = _bettingService.IsExist_PG2(false, partitionTime, data);

            if (slotDatas.Any())
            {
                //已存在指定有效投注
                message = "already exist valid bet by refId and roundId.";
                _logger.LogError($"CheckBetForBet Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckBetForBet Exception:{ex.Message}");
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
                _logger.LogError($"SetBalanceForBet Fail:{message}");
                return false;
            }
            afterAmount = (rate > 0) ? afterAmount / rate : afterAmount;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"SetBalanceForBet Exception:{ex.Message}");
            return false;
        }
    }

    #endregion

    #region payout

    private bool CheckPayout(DateTime partitionTime, BettingDataRNG_PG2 data, out string message)
    {
        message = string.Empty;
        try
        {
            var slotDatas = _bettingService.IsExist_PG2(false, partitionTime, data);

            if (slotDatas.Any())
            {
                //已存在指定有效派彩
                message = "already exist valid payout by refId and roundId.";
                _logger.LogError($"CheckPayout Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckPayout Exception:{ex.Message}");
            return false;
        }
    }

    private bool SetBalanceForPayout(TpCustInfo tpCustInfo, BettingDataRNG_PG2 data, decimal rate,
        out string message, out decimal afterAmount, out int bonusId,
        MySqlConnection conn, MySqlTransaction trans)
    {
        message = string.Empty;
        afterAmount = 0;
        bonusId = 0;
        try
        {
            var result = _settledService.SetBalance_PG2(tpCustInfo, data, TransType.Payout, "Payout", out afterAmount, ref bonusId, conn, trans);

            if (!result)
            {
                message = "set balance fail.";
                _logger.LogError($"SetBalanceForPayout Fail:{message}");
                return false;
            }
            afterAmount = (rate != 0) ? afterAmount / rate : afterAmount;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"SetBalanceForPayout Exception:{ex.Message}");
            return false;
        }
    }

    #endregion

    #region cancel

    private bool CheckCancel(DateTime partitionTime, BettingDataRNG_PG2 data, out int sq, out string message)
    {
        message = string.Empty;
        sq = 0;
        try
        {
            var slotDatas = _bettingService.IsExist_PG2(false, partitionTime, data);

            if (!slotDatas.Any())
            {
                //不存在指定有效投注
                message = "not exist bet by refId and roundId.";
                _logger.LogError($"CheckCancel Fail:{message}");
                return false;
            }

            if (slotDatas.Count() > 1)
            {
                //存在多筆指定有效投注
                message = "already exist multi bet by refId and roundId.";
                _logger.LogError($"CheckCancel Fail:{message}");
                return false;
            }

            var endRoundDatas = _bettingService.IsExist_PG2(true, partitionTime, data);

            if (endRoundDatas.Any())
            {
                //該回合已結束，不可取消投注
                message = "roundId already end. can't cancel this round's bet.";
                _logger.LogError($"CheckCancel Fail:{message}");
                return false;
            }

            sq = slotDatas.First().SQ;
            return true;

        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckCancel Exception:{ex.Message}");
            return false;
        }
    }

    private bool SetBalanceForCancel(TpCustInfo tpCustInfo, BettingDataRNG_PG2 data, decimal rate,
        out string message, out decimal afterAmount, out int bonusId,
        MySqlConnection conn, MySqlTransaction trans)
    {
        message = string.Empty;
        afterAmount = 0;
        bonusId = 0;
        try
        {
            var result = _settledService.SetBalance_PG2(tpCustInfo, data, TransType.Refunds, "CancelBet", out afterAmount, ref bonusId, conn, trans);

            if (!result)
            {
                message = "set balance fail.";
                _logger.LogError($"SetBalanceForCancel Fail:{message}");
                return false;
            }
            afterAmount = (rate > 0) ? afterAmount / rate : afterAmount;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"SetBalanceForCancel Exception:{ex.Message}");
            return false;
        }
    }

    #endregion

    #region common

    private static string GetCurrentGameTime()
    {
        var currentAt = new DateTimeOffset(DateTime.Now);
        return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
    }

    private bool GetTpCustInfo(string username, out TpCustInfo tpCustInfo, out string message)
    {
        message = string.Empty;
        tpCustInfo = null;
        try
        {
            tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)_thisTp, username);
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
            _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.Message}");
            return false;
        }
    }

    private bool GetTpCurrency(int currencyId, out TpCurrency tpCurrency, out string message)
    {
        message = string.Empty;
        tpCurrency = null;
        try
        {
            tpCurrency = _thirdPartyBll.GetTpCurrency((int)_thisTp, currencyId);
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
            _logger.LogError(ex, $"GetTpCurrency Exception:{ex.Message}");
            return false;
        }
    }

    private bool GetBalance(string username, out decimal beforeBalance, out string message)
    {
        message = string.Empty;
        beforeBalance = 0;
        try
        {
            var result = _tpCustInfoBll.GetBalance((int)_thisTp, username, (int)_gameType);

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
            _logger.LogError(ex, $"GetBalance Exception:{ex.Message}");
            return false;
        }
    }

    private bool CheckBeforeBalance(decimal betAmount, decimal beforeBalance, out string message)
    {
        message = string.Empty;

        if (beforeBalance - betAmount < 0)
        {
            message = "Balance insufficient.";
            _logger.LogError($"CheckBeforeBalance Fail:{message}");
            return false;
        }

        return true;
    }

    private bool InsertBettrans(BettransType bettransType, DateTime partitionTime, BettingDataRNG_PG2 data, out string message, MySqlTransaction trans)
    {
        message = string.Empty;
        var result = false;

        try
        {
            if (bettransType == BettransType.Bet)
            {
                result = _bettingService.InsertBettransRng_PG2(partitionTime, data, trans);
            }
            else if (bettransType == BettransType.Payout)
            {
                result = _settledService.InsertBettrans_PG2(partitionTime, data, trans);
            }

            if (!result)
            {
                message = $"insert bettrans fail {bettransType}.";
                _logger.LogError($"InsertBettrans Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"InsertBettrans Exception:{ex.Message}");
            return false;
        }
    }

    private bool UpdateBettrans(DateTime partitionTime, int sq, out string message, MySqlTransaction trans)
    {
        message = string.Empty;
        try
        {
            var result = _settledService.CancelBettrans_PG2(partitionTime, sq, trans);

            if (!result)
            {
                message = "cancel fail.";
                _logger.LogError($"UpdateBettrans Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"UpdateBettrans Exception:{ex.Message}");
            return false;
        }
    }

    #endregion

}