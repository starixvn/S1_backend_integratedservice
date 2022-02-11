using System;
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

        public async Task<HBPlayerDetailResponse> Auth(HBPlayerDetailRequest request)
        {
            var response = new HBPlayerDetailResponse()
            {
                playerdetailresponse = new playerdetailresponse()
            };
            var status = new HBStatus()
            {
                success = false,
                autherror = true
            };
            
            if (!string.IsNullOrEmpty(request.playerdetailrequest.token))
            {
                var (authToken, sessionToken) = ParseToken(request.playerdetailrequest.token);
                if (_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null || tpCheck.Token != sessionToken)
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
                            response.playerdetailresponse.Status = new HBStatus()
                            {
                                success = true,
                                autherror = false,
                                message = ""
                            };
                            response.playerdetailresponse.accountid = tpCustInfoAuth.TpUserName;
                            response.playerdetailresponse.accountname = tpCustInfoAuth.TpUserName;
                            response.playerdetailresponse.balance = tpCustBalance.Balance;
                            response.playerdetailresponse.currencycode = tpCustBalance.TpCurrencyId;
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
            response.playerdetailresponse.Status = status;
            return response;
        }

        public async Task<Fundtransferresponse> FundTransfer(Fundtransferrequest request)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.HB, request.accountid);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.HB, tpCustInfo.CurrencyId);

            var afterAmount = 0m;
            var result = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();
            
            foreach (var fundInfo in request.funds.fundinfo)
            {
                if (tpCurrency.ExchangeRate > 0)
                    fundInfo.amount *= tpCurrency.ExchangeRate;

                var bettingData_HB = new BettingDataRNG_HB()
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
                    dtevent = DateTime.Parse(fundInfo.dtevent),
                    initialdebittransferid = fundInfo.initialdebittransferid,
                    accounttransactiontype = fundInfo.accounttransactiontype,
                    gameinfeature = fundInfo.gameinfeature,
                    keyname = request.gamedetails.keyname,
                    gametypename = request.gamedetails.gametypename,
                    channel = request.gamedetails.channel
                };
                
                // debit
                if (fundInfo.amount < 0)
                {
                    result = _bettingService.PlaceBet_HB(tpCustInfo, bettingData_HB, out afterAmount, conn, trans);
                    if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                    {
                        afterAmount /= tpCurrency.ExchangeRate;
                    }
                }
                // credit
                else
                {
                    result = _settledService.PayoutRNG_HB(tpCustInfo, bettingData_HB, conn, trans);
                    if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                    {
                        afterAmount /= tpCurrency.ExchangeRate;
                    }
                }
            }

            if (result > 0)
            {
                trans.Commit();
                return new Fundtransferresponse()
                {
                    status = new Status()
                    {
                        success = true,
                        successdebit = true,
                        successcredit = true
                    },
                    balance = afterAmount,
                    currencycode = tpCurrency.TpCurrencyId
                };
            }

            return new Fundtransferresponse()
            {
                status = new Status()
                {
                    success = false,
                    nofunds = true
                },
                currencycode = tpCurrency.TpCurrencyId
            };
        }

        private (string authToken, string sessionToken) ParseToken(string token)
        {
            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(token)));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return sessionKeyArr.Length > 2 ? (sessionKeyArr[0], sessionKeyArr[1]) : (string.Empty, string.Empty);
        }
    }
}