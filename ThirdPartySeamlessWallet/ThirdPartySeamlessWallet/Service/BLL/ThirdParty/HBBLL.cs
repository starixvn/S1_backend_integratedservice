using System.Dynamic;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class HBBLL : IHBBLL
    {
        private readonly Tp _thisTp;
        public readonly GameType _GameType;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<HBBLL> _logger;
        private readonly MySqlClient _mainDbClient;

        public HBBLL(IServiceProvider provider)
        {
            _thisTp = Tp.HB;
            _GameType = GameType.HB_RNG;
            _authBLL = provider.GetService<AuthBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _logger = provider.GetService<ILogger<HBBLL>>();
        }

        public playerdetailresponse PlayerDetail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return PlayerDetailResponse($"Player session token is empty");
            }

            var authToken = ParseToken(token);
            if (authToken == null)
            {
                return PlayerDetailResponse($"Invalid player session");
            }

            var tpUserName = authToken.tpUserName;

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpUserName);
            if (tpCheck == null || tpCheck.Token != authToken.longTimeToken)
            {
                _logger.LogError($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player session is expired");
                return PlayerDetailResponse($"Player session is expired");
            }
            else if (tpCheck.Locked)
            {
                return PlayerDetailResponse($"Player is inactive");
            }
            else
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpUserName, (int)_GameType);
                if (tpCustBalance != null)
                {
                    return PlayerDetailResponse("", tpUserName, tpCustBalance.Balance, tpCustBalance.TpCurrencyId);
                }
                return PlayerDetailResponse($"Can't get player:{tpUserName} balance");
            }
        }

        private static playerdetailresponse PlayerDetailResponse(string message, string tpUserName = "", decimal balance = 0m, string currency = "")
        {
            if (string.IsNullOrEmpty(message))
            {
                return new playerdetailresponse()
                {
                    Status = new HBStatus()
                    {
                        success = true,
                        autherror = false,
                        message = message
                    },
                    accountid = tpUserName,
                    accountname = tpUserName,
                    balance = balance,
                    currencycode = currency
                };
            }

            return new playerdetailresponse()
            {
                Status = new HBStatus()
                {
                    success = false,
                    autherror = true,
                    message = message
                }
            };
        }

        public bool Auth(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                var authToken = ParseToken(token);
                if (authToken == null)
                {
                    return false;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, authToken.tpUserName);
                if (tpCheck == null)
                {
                    _logger.LogError($"tpCheck:{JsonConvert.SerializeObject(authToken)}, wrong tpUser");
                    return false;
                }
                else if (tpCheck.Token != authToken.longTimeToken)
                {
                    _logger.LogError($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player session is expired");
                    return false;
                }
                else if (tpCheck.Locked)
                {
                    _logger.LogError($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player is inactive");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                _logger.LogError("Player session token is empty");
                return false;
            }
        }

        public async Task<dynamic> FundTransfer(dynamic request)
        {
            var tpUserName = (string)request.accountid;
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, tpUserName);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            // 退款
            if ((bool)request.isretry && (bool)request.isrefund)
            {
                return await Refund(request, tpCustInfo, tpCurrency);
            }
            else
            {
                return await DebitAndCredit(request, tpCustInfo, tpCurrency);
            }
        }

        private async Task<dynamic> DebitAndCredit(dynamic request, TpCustInfo tpCustInfo, TpCurrency tpCurrency)
        {
            var transactionResult = 0;
            dynamic response = new ExpandoObject();
            var bonusId = 0;
            var afterAmount = 0m;
            var transId = "";

            // 檢查同時存在 bet & payout的下注額度是否足夠
            if (request.funds.debitandcredit == true)
            {
                var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, (string)request.accountid, (int)_GameType);
                var tid = request.friendlygameinstanceid.ToString();
                // 加總下注金額
                Funds funds = JsonConvert.DeserializeObject<Funds>(JsonConvert.SerializeObject(request.funds));
                var stake = funds.fundinfo.Where(x => x.amount < 0).Sum(x => x.amount);
                if (tpCurrency.ExchangeRate != 0)
                {
                    stake *= tpCurrency.ExchangeRate;
                    custBalance.Balance *= tpCurrency.ExchangeRate;
                }

                if (custBalance.Balance < 0)
                {
                    response.status = new ExpandoObject();
                    response.status.success = false;
                    response.status.nofunds = true;
                    afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                    var TpCheckRecordParam = GetTpBetFailureParam_HB(tpCustInfo, custBalance, tpCurrency, funds, TpCheckRecordCode.InsufficientBalance, afterAmount, tid);
                    _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                    return response;
                }
                var checkBalance = custBalance.Balance - Math.Abs(stake);
                if (checkBalance < 0)
                {
                    response.status = new ExpandoObject();
                    response.status.success = false;
                    response.status.nofunds = true;
                    afterAmount = checkBalance / tpCurrency.ExchangeRate;
                    var TpCheckRecordParam = GetTpBetFailureParam_HB(tpCustInfo, custBalance, tpCurrency, funds, TpCheckRecordCode.InsufficientBalance, afterAmount, tid);
                    _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                    return response;
                }
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            foreach (var fundInfo in request.funds.fundinfo)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    fundInfo.amount *= tpCurrency.ExchangeRate;
                    fundInfo.bonusamount *= tpCurrency.ExchangeRate;
                    fundInfo.jpcont *= tpCurrency.ExchangeRate;
                }

                // debit
                if (fundInfo.amount < 0)
                {
                    var placeBetRngDto = new PlaceBetRngDto_HB()
                    {
                        accountid = request.accountid,
                        gameinstanceid = request.gameinstanceid,
                        friendlygameinstanceid = request.friendlygameinstanceid,
                        gamestatemode = fundInfo.gamestatemode,
                        transferid = fundInfo.transferid,
                        currencycode = fundInfo.currencycode,
                        amount = fundInfo.amount,
                        bonusamount = fundInfo.bonusamount,
                        jpwin = fundInfo.jpwin,
                        jpcont = fundInfo.jpcont,
                        isbonus = fundInfo.isbonus,
                        dtevent = DateTimeOffset.Parse((string)fundInfo.dtevent).DateTime,
                        initialdebittransferid = fundInfo.initialdebittransferid,
                        accounttransactiontype = fundInfo.accounttransactiontype,
                        gameinfeature = fundInfo.gameinfeature,
                        gamename = request.gamedetails.name,
                        keyname = request.gamedetails.keyname,
                        gametypename = request.gamedetails.gametypename,
                        channel = request.gamedetails.channel
                    };
                    transId = placeBetRngDto.friendlygameinstanceid.ToString();
                    transactionResult = _bettingService.PlaceBet_HB(tpCustInfo, placeBetRngDto, out afterAmount, ref bonusId, conn, trans);
                }
                // credit
                else
                {
                    var payoutRngDto = new PayoutRngDto_HB()
                    {
                        accountid = request.accountid,
                        gameinstanceid = request.gameinstanceid,
                        friendlygameinstanceid = request.friendlygameinstanceid,
                        gamestatemode = fundInfo.gamestatemode,
                        transferid = (bool)request.isrecredit ? fundInfo.originaltransferid : fundInfo.transferid,
                        currencycode = fundInfo.currencycode,
                        amount = fundInfo.amount,
                        bonusamount = fundInfo.bonusamount,
                        jpwin = fundInfo.jpwin,
                        jpcont = fundInfo.jpcont,
                        isbonus = fundInfo.isbonus,
                        dtevent = DateTimeOffset.Parse((string)fundInfo.dtevent).DateTime,
                        initialdebittransferid = (bool)fundInfo.isbonus ? request.bonusdetails.bonusbalanceid : fundInfo.initialdebittransferid,
                        accounttransactiontype = fundInfo.accounttransactiontype,
                        gameinfeature = fundInfo.gameinfeature,
                        gamename = request.gamedetails.name,
                        keyname = request.gamedetails.keyname,
                        gametypename = request.gamedetails.gametypename,
                        channel = request.gamedetails.channel
                    };
                    transId = payoutRngDto.friendlygameinstanceid.ToString();
                    transactionResult = _settledService.PayoutRNG_HB(tpCustInfo, payoutRngDto, out afterAmount, ref bonusId, conn, trans);
                }

                if (afterAmount < 0)
                {
                    _logger.LogError($"user {tpCustInfo.TpUserName} afterAmount:{afterAmount} < 0");
                    break;
                }
            }

            if (transactionResult > 0)
            {
                if (afterAmount == 0)
                {
                    afterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, (string)request.accountid, (int)_GameType).Balance;
                }
                else
                {
                    if (tpCurrency.ExchangeRate > 0)
                        afterAmount /= tpCurrency.ExchangeRate;
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                if (bonusId > 0)
                {
                    _logger.LogInformation($"user {tpCustInfo.TpUserName} has bonus wallet bonusId:{bonusId} transId:{transId}");
                    _settledService.UpdateTpBonusFlag_HB((int)_thisTp, transId, (int)GameType.HB_RNG, TpCategory.Slots, out int CustId, out bonusId);
                }
                response.status = new ExpandoObject();
                response.status.success = true;
                response.status.successdebit = true;
                response.status.successcredit = true;
            }
            else
            {
                response.status = new ExpandoObject();
                response.status.success = false;
                response.status.nofunds = true;
                afterAmount = _tpCustInfoBLL.GetBalance((int)_thisTp, (string)request.accountid, (int)_GameType).Balance;
            }

            response.balance = afterAmount;
            response.currencycode = tpCurrency.TpCurrencyId;
            return response;
        }

        private async Task<dynamic> Refund(dynamic request, TpCustInfo tpCustInfo, TpCurrency tpCurrency)
        {
            dynamic response = new ExpandoObject();
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            var cancelBet = new CancelBetRngDto_HB()
            {
                AccountId = request.accountid,
                TransferId = request.funds.refund.transferid,
                BetId = request.funds.refund.originaltransferid,
                Amount = request.funds.refund.amount,
                dtevent = request.funds.refund.dtevent
            };
            var transactionResult = _settledService.CancelBetRNG_HB(tpCustInfo, cancelBet, conn, trans);

            if (transactionResult.result > 0)
            {
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                response.status = new ExpandoObject();
                response.status.success = true;
                response.status.refundstatus = 1;
            }
            else
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            }

            if (transactionResult.result == -2)
            {
                response.status = new ExpandoObject();
                response.status.success = true;
                response.status.refundstatus = 2;
            }

            response.balance = _tpCustInfoBLL.GetBalance((int)_thisTp, (string)request.accountid, (int)_GameType).Balance;
            response.currencycode = tpCurrency.TpCurrencyId;

            return response;
        }

        public bool QueryRequest(dynamic request)
        {
            return _bettingService.QueryBettrans((string)request.transferid);
        }

        private TokenInfo ParseToken(string token)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(token)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                if (sessionKeyArr.Length > 2)
                {
                    return new TokenInfo
                    {
                        shortTimeToken = sessionKeyArr[0], /* 1 min*/
                        longTimeToken = sessionKeyArr[1],  /* Long Time*/
                        tpUserName = sessionKeyArr[2],
                    };
                }
                return null;
            }
            catch (FormatException e)
            {
                _logger.LogError(e, $"invalid token:{token}");
                return null;
            }
        }

        private static TpBetFailureRecord GetTpBetFailureParam_HB(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, Funds funds, TpCheckRecordCode Code, decimal AfterAmount, string tid)
        {
            // BLL會經過匯率換算 下列還原為原始資料
            TpBetFailureRecordInfo_HB InfoData = new()
            {
                CurrentBalance = custBalance.Balance / tpCurrency.ExchangeRate,
                BetAmount = funds.fundinfo.Where(x => x.amount < 0).Sum(x => x.amount) / tpCurrency.ExchangeRate,
                WinAmount = funds.fundinfo.Where(x => x.amount > 0).Sum(x => x.amount) / tpCurrency.ExchangeRate,
                InsufficientBalance = AfterAmount,
                ExchangeRate = tpCurrency.ExchangeRate,
            };

            return new TpBetFailureRecord
            {
                TransId = tid,
                TpId = (int)Tp.HB,
                TpGameType = (int)GameType.HB_RNG,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }
    }
}