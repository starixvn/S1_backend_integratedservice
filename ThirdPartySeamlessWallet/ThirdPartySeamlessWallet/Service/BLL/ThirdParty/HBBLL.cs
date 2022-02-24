using System;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
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
        private readonly Tp thisTp;
        public readonly GameType GameType;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<HBBLL> _logger;
        private readonly MySqlClient _mainDbClient;

        public HBBLL(IServiceProvider provider)
        {
            thisTp = Tp.HB;
            GameType = GameType.HB_RNG;
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
            var response = new playerdetailresponse();
            var status = new HBStatus()
            {
                success = false,
                autherror = true
            };
            
            if (!string.IsNullOrEmpty(token))
            {
                var authToken = ParseToken(token);
                if (authToken != null && _authBLL.Authenticate(authToken.shortTimeToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null || tpCheck.Token != authToken.longTimeToken)
                    {
                        _logger.LogInformation($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player session is expired");
                        status.message = "player session is expired";
                    }
                    else if (tpCheck.Locked)
                    {
                        status.message = "player is inactive";
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, (int)GameType);
                        if (tpCustBalance != null)
                        {
                            response.Status = new HBStatus()
                            {
                                success = true,
                                autherror = false,
                                message = ""
                            };
                            response.accountid = tpCustInfoAuth.TpUserName;
                            response.accountname = tpCustInfoAuth.TpUserName;
                            response.balance = tpCustBalance.Balance;
                            response.currencycode = tpCustBalance.TpCurrencyId;
                            return response;
                        }
                    }
                }
                else
                {
                    status.message = "Invalid player session";
                }
            }
            else
            {
                status.message = "Player session token is empty";
            }
            response.Status = status;
            return response;
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

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, authToken.tpUserName);
                if (tpCheck == null)
                {
                    _logger.LogInformation($"tpCheck:{JsonConvert.SerializeObject(authToken)}, wrong tpUser");
                    return false;
                }
                else if(tpCheck.Token != authToken.longTimeToken)
                {
                    _logger.LogInformation($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player session is expired");
                    return false;
                }
                else if (tpCheck.Locked)
                {
                    _logger.LogInformation($"tpCheck:{JsonConvert.SerializeObject(tpCheck)}, player is inactive");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                _logger.LogInformation("Player session token is empty");
                return false;
            }
        }
        
        public async Task<dynamic> FundTransfer(dynamic request)
        {
            var tpUserName = (string) request.accountid;
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.HB, tpUserName);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.HB, tpCustInfo.CurrencyId);

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

        public async Task<dynamic> DebitAndCredit(dynamic request, TpCustInfo tpCustInfo, TpCurrency tpCurrency)
        {
            var transactionResult = 0;
            dynamic response = new ExpandoObject();
            var bonusId = 0;
            var afterAmount = 0m;
            var transId = "";
            
            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();
            foreach (var fundInfo in request.funds.fundinfo)
            {
                if (tpCurrency.ExchangeRate > 0)
                    fundInfo.amount *= tpCurrency.ExchangeRate;
                
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
                        keyname = request.gamedetails.keyname,
                        gametypename = request.gamedetails.gametypename,
                        channel = request.gamedetails.channel
                    };
                    transId = placeBetRngDto.initialdebittransferid;
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
                        transferid = (bool) request.isrecredit ? fundInfo.originaltransferid : fundInfo.transferid,
                        currencycode = fundInfo.currencycode,
                        amount = fundInfo.amount,
                        bonusamount = fundInfo.bonusamount,
                        jpwin = fundInfo.jpwin,
                        jpcont = fundInfo.jpcont,
                        isbonus = fundInfo.isbonus,
                        dtevent = DateTimeOffset.Parse((string) fundInfo.dtevent).DateTime,
                        initialdebittransferid = (bool) fundInfo.isbonus ? request.bonusdetails.bonusbalanceid : fundInfo.initialdebittransferid,
                        accounttransactiontype = fundInfo.accounttransactiontype,
                        gameinfeature = fundInfo.gameinfeature,
                        keyname = request.gamedetails.keyname,
                        gametypename = request.gamedetails.gametypename,
                        channel = request.gamedetails.channel
                    };
                    transId = payoutRngDto.initialdebittransferid;
                    transactionResult = _settledService.PayoutRNG_HB(tpCustInfo, payoutRngDto, out afterAmount, ref bonusId, conn, trans);
                }

                if (afterAmount < 0)
                {
                    _logger.LogInformation($"user {tpCustInfo.TpUserName} afterAmount:{afterAmount} < 0");
                    break;
                }
            }
            
            if (transactionResult > 0)
            {
                if (afterAmount == 0)
                {
                    afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.HB, (string)request.accountid, (int)GameType).Balance;
                }
                else
                {
                    if (tpCurrency.ExchangeRate > 0) 
                        afterAmount /= tpCurrency.ExchangeRate;
                }
                
                trans.Commit();
                if (bonusId > 0)
                {
                    _logger.LogInformation($"user {tpCustInfo.TpUserName} has bonus wallet bonusId:{bonusId} transId:{transId}");
                    _settledService.UpdateTpBonusFlag_HB((int) Tp.HB, transId, (int) GameType.HB_RNG, TpCategory.Slots, out int CustId, out bonusId);
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
                afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.HB, (string)request.accountid, (int)GameType).Balance;
            }

            response.balance = afterAmount;
            response.currencycode = tpCurrency.TpCurrencyId;
            return response;
        }

        public async Task<dynamic> Refund(dynamic request, TpCustInfo tpCustInfo, TpCurrency tpCurrency)
        {
            dynamic response = new ExpandoObject();
            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();
            var cancelBet = new CancelBetRngDto_HB()
            {
                AccountId = request.accountid,
                TransferId = request.funds.refund.transferid,
                RoundId = request.funds.refund.originaltransferid,
                Amount = request.funds.refund.amount,
                dtevent = request.funds.refund.dtevent
            };
            var transactionResult = _settledService.CancelBetRNG_HB(tpCustInfo, cancelBet, conn, trans);

            if (transactionResult.result > 0)
            {
                trans.Commit();
                response.status = new ExpandoObject();
                response.status.success = true;
                response.status.refundstatus = 1;
            }

            if (transactionResult.result == -2)
            {
                response.status = new ExpandoObject();
                response.status.success = true;
                response.status.refundstatus = 2;
            }

            response.balance = _tpCustInfoBLL.GetBalance((int)Tp.HB, (string)request.accountid, (int)GameType).Balance;
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
                _logger.LogError($"invalid token:{token}");
                return null;
            }
        }
    }
}