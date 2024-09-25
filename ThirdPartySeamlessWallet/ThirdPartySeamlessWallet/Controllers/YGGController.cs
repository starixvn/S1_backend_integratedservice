using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class YGGController : ControllerBase
    {
        #region Fields

        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly IHostEnvironment _environment;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;
        private readonly Dictionary<int, YGGConfig> _siteConfigDic;

        #endregion

        #region Constructors

        public YGGController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _thisTp = Tp.YGG;
            _gameType = (int)GameType.YGG_RNG;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YGGConfig>(_thisTp.ToString());
        }

        #endregion

        #region Models

        #region PlayerInfo

        /// <summary>
        /// Player Info Reply Model
        /// </summary>
        public class PlayerInfoRep
        {
            public int code { get; set; }
            public PlayerInfoData data { get; set; }
        }

        /// <summary>
        /// Player Info Reply Data
        /// </summary>
        public class PlayerInfoData
        {
            public string playerId { get; set; }
            public string nickName { get; set; }
            public string organization { get; set; }
            public decimal balance { get; set; }
            public decimal applicableBonus { get; set; }
            public string currency { get; set; }
            public string homeCurrency { get; set; }
        }

        #endregion

        #region Wager、EndWager、AppendWager、CampaignPayout

        /// <summary>
        /// Wager Request Model
        /// </summary>
        public class WagerReq : BettingDataRNG_YGG
        {
            public string org { get; set; }
            public string sessiontoken { get; set; }
            public string service { get; set; }
            public string lang { get; set; }
            public int version { get; set; }
        }

        /// <summary>
        /// Wager Reply Model
        /// </summary>
        public class WagerRep
        {
            public int code { get; set; }
            public WagerData data { get; set; }
        }

        /// <summary>
        /// Wager Reply Data
        /// </summary>
        public class WagerData
        {
            public string organization { get; set; }
            public string playerId { get; set; }
            public decimal balance { get; set; }
            public decimal applicableBonus { get; set; }
            public string currency { get; set; }
            public string homeCurrency { get; set; }

            public string bonus => "0";
            public string country { get; set; } = string.Empty;
        }

        #endregion

        #region CancelWager

        /// <summary>
        /// Cancel Wager Reply Model
        /// </summary>
        public class CancelwagerRep
        {
            public int code { get; set; }
            public CancelwagerData data { get; set; }
        }

        /// <summary>
        /// Cancel Wager Reply Data
        /// </summary>
        public class CancelwagerData
        {
            public string playerId { get; set; }
            public string organization { get; set; }
            public decimal balance { get; set; }
            public string currency { get; set; }

            public string bonus => "0";
        }

        #endregion

        #region GetBalance

        /// <summary>
        /// Get Balance Request Model
        /// </summary>
        public class GetbalanceReq
        {
            public string org { get; set; }
            public string playerid { get; set; }
            public string sessiontoken { get; set; }
        }

        /// <summary>
        /// Get Balance Reply Model
        /// </summary>
        public class GetbalanceRep
        {
            public int code { get; set; }
            public GetbalanceData data { get; set; }
        }

        /// <summary>
        /// Get Balance Reply Data
        /// </summary>
        public class GetbalanceData
        {
            public string currency { get; set; }
            public decimal applicableBonus { get; set; }
            public string homeCurrency { get; set; }
            public string organization { get; set; }
            public decimal balance { get; set; }
            public string nickName { get; set; }
            public string playerId { get; set; }
            public decimal bonus { get; set; }
        }

        #endregion

        #endregion

        #region Request

        public class PlayerInfoReq
        {
            public string sessiontoken { get; set; }
            public string cat1 { get; set; }
            public string cat2 { get; set; }
            public string cat3 { get; set; }
            public string cat4 { get; set; }
            public string cat5 { get; set; }
            public string cat6 { get; set; }
            public object cat7 { get; set; }
            public object cat8 { get; set; }
            public object cat9 { get; set; }
            public string tag1 { get; set; }
            public string tag2 { get; set; }
            public string tag3 { get; set; }
            public object tag4 { get; set; }
            public object tag5 { get; set; }
            public object tag6 { get; set; }
            public object tag7 { get; set; }
            public object tag8 { get; set; }
            public object tag9 { get; set; }
            public string lang { get; set; }
            public int version { get; set; }
            public string org { get; set; }
        }

        #endregion

        [Route("[controller]/playerinfo.json")]
        public async Task<PlayerInfoRep> PlayerInfoAsync()
        {
            var logName = $"{_thisTp}_playerinfo";
            var playerInfoRep = new PlayerInfoRep
            {
                code = 1
            };

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            var playerInfoReq = JsonConvert.DeserializeObject<PlayerInfoReq>(Content);
            if (string.IsNullOrEmpty(playerInfoReq.sessiontoken))
            {
                playerInfoRep.code = 1000;
                _log2File.Error($"{JsonConvert.SerializeObject(playerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}", logName);
                return playerInfoRep;
            }

            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(playerInfoReq.sessiontoken)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var authToken = sessionKeyArr[0]; /* 1 Time*/
                var sessionToken = sessionKeyArr[1];/* Long Time*/

                if (_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null)
                    {
                        playerInfoRep.code = 1000;
                    }
                    else if (tpCheck.Token != sessionToken)
                    {
                        playerInfoRep.code = 1000;
                    }
                    else if (tpCheck.Locked)
                    {
                        playerInfoRep.code = 1007;
                    }
                    else
                    {
                        var thirdPartySetting = _siteConfigDic[tpCustInfoAuth.SiteId];
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
                        var playerInfoData = new PlayerInfoData()
                        {
                            balance = tpCustBalance.Balance,
                            playerId = tpCustInfoAuth.TpUserName,
                            organization = thirdPartySetting.org,
                            currency = tpCustInfoAuth.TpCurrencyId,
                            homeCurrency = tpCustInfoAuth.TpCurrencyId,
                            applicableBonus = 0
                        };
                        playerInfoRep.data = playerInfoData;
                        playerInfoRep.code = 0;
                    }
                }
                else
                {
                    playerInfoRep.code = 1000;
                }
            }
            catch (Exception ex)
            {
                playerInfoRep.code = 1000;
                _log2File.Error($"{JsonConvert.SerializeObject(playerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}\r\n{ex}", logName, ex);
            }


            _log2File.Info($"{JsonConvert.SerializeObject(playerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}", logName);
            return playerInfoRep;
        }

        /// <summary>
        /// 下注
        /// </summary>
        /// <param name="wagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/wager.json")]
        public async Task<WagerRep> WagerAsync()
        {
            var logName = $"{_thisTp}_wager";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            WagerRep wagerRep;
            var statusCode = 1;
            var wagerData = new WagerData();


            var wagerReq = JsonConvert.DeserializeObject<WagerReq>(Content);

            if (string.IsNullOrEmpty(wagerReq.sessiontoken))
            {
                wagerRep = new WagerRep()
                {
                    code = 1000,
                    data = wagerData
                };
                _log2File.Error($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
                return wagerRep;
            }

            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(wagerReq.sessiontoken)));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            var sessionToken = sessionKeyArr[1];/* Long Time*/

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, wagerReq.playerid);
            if (tpCheck == null)
            {
                statusCode = 1008;
            }
            else if (tpCheck.Token != sessionToken)
            {
                statusCode = 1008;
            }
            else if (tpCheck.Locked)
            {
                statusCode = 1007;
            }
            else
            {
                wagerData = new WagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = wagerReq.org,
                    currency = wagerReq.currency,
                    homeCurrency = wagerReq.currency,
                    applicableBonus = 0
                };

                try
                {
                    var result = _bettingBLL.PlaceBetRNG_YGG(wagerReq, out decimal tpAfterAmount, out string countryCode);
                    if (result > 0)
                    {
                        if (wagerReq.amount == 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, wagerReq.playerid, _gameType);
                            wagerData.balance = tpCustBalance.Balance;
                        }
                        else
                        {
                            wagerData.balance = tpAfterAmount;
                        }
                        wagerData.country = countryCode;
                        statusCode = 0;
                    }
                    else if (tpAfterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, wagerReq.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 1006;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, wagerReq.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                    else
                    {
                        wagerRep = new WagerRep()
                        {
                            code = 1,
                            data = wagerData
                        };

                        _log2File.Error($"result = {result}\r\n{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
                        return wagerRep;
                    }
                }
                catch (Exception ex)
                {
                    wagerRep = new WagerRep()
                    {
                        code = 1,
                        data = wagerData
                    };

                    _log2File.Error($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", logName, ex);
                    return wagerRep;
                }
            }

            wagerRep = new WagerRep()
            {
                code = statusCode,
                data = wagerData
            };

            _log2File.Info($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);

            return wagerRep;
        }

        /// <summary>
        /// 结算
        /// </summary>
        /// <param name="endWagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/endwager.json")]
        public async Task<WagerRep> EndWagerAsync()
        {
            var logName = $"{_thisTp}_endwager";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            WagerRep wagerRep;
            var statusCode = 1;
            var wagerData = new WagerData();

            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_156"",""amount"":""0.00"",""bonusprize"":""0.00"",""currency"":""THB"",""tickets"":""1"",""service"":""Mir"",""reference"":""2004051539230100001"",""subreference"":""2004051539230100001"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            try
            {
                var payoutDataRngYgg = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(Content);
                var result = _settledBLL.PayoutRNG_YGG(payoutDataRngYgg, out decimal tpAfterAmount, out string countryCode, SettleFlag.EndGame);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRngYgg.playerid,
                    organization = payoutDataRngYgg.org,
                    currency = payoutDataRngYgg.currency,
                    homeCurrency = payoutDataRngYgg.currency,
                    applicableBonus = 0,
                    country = countryCode
                };

                if (result > 0)
                {
                    if (payoutDataRngYgg.amount > 0)
                    {
                        wagerData.balance = tpAfterAmount;
                        statusCode = 0;
                    }
                    else if (payoutDataRngYgg.amount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", logName, ex);
            }

            return wagerRep;
        }

        /// <summary>
        /// 取消下注
        /// </summary>
        /// <param name="cancelWagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/cancelwager.json")]
        public async Task<CancelwagerRep> CancelWagerAsync()
        {
            var logName = $"{_thisTp}_cancelwager";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            var cancelWagerData = new CancelwagerData();
            var statusCode = 1;

            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_127586"",""reference"":""2101291333560100000"",""subreference"":""w2101291333560100001"",""version"":""3""}";
                //cancelwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            var wagerReq = JsonConvert.DeserializeObject<WagerReq>(Content);
            var result = _settledBLL.CancelBetRNG_YGG(wagerReq, out decimal tpAfterAmount);

            if (result > 0)
            {
                var tpCustBalance = _environment.IsDevelopment()
                    ? _tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType)
                    : _tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YGG, wagerReq.playerid, _gameType);

                statusCode = 0;
                cancelWagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = wagerReq.org,
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpAfterAmount
                };

                if (tpAfterAmount == 0)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType);
                    cancelWagerData.balance = tpCustBalance.Balance;
                    cancelWagerData.currency = tpCustBalance.TpCurrencyId;
                }
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType);

                statusCode = 0;
                cancelWagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = wagerReq.org,
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpCustBalance.Balance
                };
            }
            var cancelWagerRep = new CancelwagerRep()
            {
                code = statusCode,
                data = cancelWagerData
            };

            _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(cancelWagerRep)}", logName);

            return cancelWagerRep;
        }

        /// <summary>
        /// 额外结果
        /// </summary>
        /// <param name="appendWagerResultReq"></param>
        /// <returns></returns>
        [Route("[controller]/appendwagerresult.json")]
        public async Task<WagerRep> AppendWagerResultAsync()
        {
            var logName = $"{_thisTp}_appendwagerresult";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            WagerRep wagerRep;
            var wagerData = new WagerData();
            var statusCode = 1;

            if (_environment.IsDevelopment())
            {
                /*var s = @"{""org"":""nova88"",""playerid"":""DNova88_156"",""amount"":""7136.45"",""bonusprize"":""0.00"",""currency"":""THB"",""tickets"":""0"",""service"":""Mir"",""reference"":""2004061617230100001"",""subreference"":""2004061617230100000.2004061617240100000"",""description"":""Jackpot"",""isJackpotWin"":""true"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""ShardsJ"",""cat4"":""Holmes and the Stolen Stones"",""cat5"":""7317"",""tag1"":""GameName.Holmes and the Stolen Stones"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                appendwagerresultReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);*/
            }

            try
            {
                var payoutDataRngYgg = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(Content);
                var result = _settledBLL.PayoutRNG_YGG(payoutDataRngYgg, out decimal tpAfterAmount, out string countryCode, SettleFlag.Running);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRngYgg.playerid,
                    organization = payoutDataRngYgg.org,
                    currency = payoutDataRngYgg.currency,
                    homeCurrency = payoutDataRngYgg.currency,
                    applicableBonus = 0,
                    country = countryCode
                };

                if (result > 0)
                {
                    if (payoutDataRngYgg.amount > 0)
                    {
                        wagerData.balance = tpAfterAmount;
                        statusCode = 0;
                    }
                    else if (payoutDataRngYgg.amount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", logName, ex);
            }

            return wagerRep;
        }

        /// <summary>
        /// 活动支出
        /// </summary>
        /// <param name="campaignPayoutReq"></param>
        /// <returns></returns>
        [Route("[controller]/campaignpayout.json")]
        public async Task<WagerRep> CampaignPayoutAsync()
        {
            var logName = $"{_thisTp}_appendwagerresult";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            var wagerRep = new WagerRep
            {
                code = 1
            };

            var wagerReq = JsonConvert.DeserializeObject<WagerReq>(Content);

            if (string.IsNullOrEmpty(wagerReq.sessiontoken))
            {
                wagerRep.code = 1000;
                _log2File.Error($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
                return wagerRep;
            }

            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(wagerReq.sessiontoken)));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            var sessionToken = sessionKeyArr[1];/* Long Time*/

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, wagerReq.playerid);
            if (tpCheck == null)
            {
                wagerRep.code = 1008;
            }
            if (tpCheck.Token != sessionToken)
            {
                wagerRep.code = 1008;
            }
            else if (tpCheck.Locked)
            {
                wagerRep.code = 1007;
            }

            _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(wagerRep)}", logName);
            return wagerRep;
        }

        /// <summary>
        /// 取得余额
        /// </summary>
        /// <param name="getBalanceReq"></param>
        /// <returns></returns>
        [Route("[controller]/getbalance.json")]
        public async Task<GetbalanceRep> GetBalanceAsync()
        {
            var logName = $"{_thisTp}_getbalance";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            var balanceReq = JsonConvert.DeserializeObject<GetbalanceReq>(Content);

            var getBalanceRep = new GetbalanceRep();
            var statusCode = 1;
            var getBalanceData = new GetbalanceData();

            if (string.IsNullOrEmpty(balanceReq.sessiontoken))
            {
                getBalanceRep = new GetbalanceRep()
                {
                    code = statusCode,
                    data = getBalanceData
                };
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceRep)}", logName);
                return getBalanceRep;
            }

            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(balanceReq.sessiontoken)));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            var authToken = sessionKeyArr[0]; /* 1 Time*/
            var sessionToken = sessionKeyArr[1];/* Long Time*/

            if (_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
            {

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, balanceReq.playerid);
                if (tpCheck == null)
                {
                    statusCode = 1008;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    statusCode = 1008;
                }
                else if (tpCheck.Locked)
                {
                    statusCode = 1007;
                }
                else
                {

                    var thirdPartySetting = _siteConfigDic[tpCustInfoAuth.SiteId];
                    ;
                    var tpCustBalance =
                        _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, balanceReq.playerid, _gameType);

                    getBalanceData = new GetbalanceData()
                    {
                        currency = tpCustBalance.TpCurrencyId,
                        applicableBonus = 0,
                        homeCurrency = tpCustBalance.TpCurrencyId,
                        organization = thirdPartySetting.org,
                        balance = tpCustBalance.Balance,
                        playerId = balanceReq.playerid,
                        bonus = 0
                    };

                    statusCode = 0;
                }
            }
            else
            {
                getBalanceRep.code = 1000;
            }

            getBalanceRep = new GetbalanceRep()
            {
                code = statusCode,
                data = getBalanceData
            };

            _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceRep)}", logName);
            return getBalanceRep;
        }
    }
}