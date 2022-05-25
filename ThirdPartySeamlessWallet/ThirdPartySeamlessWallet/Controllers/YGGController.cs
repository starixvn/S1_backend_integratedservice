using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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

        private readonly AuthBLL _authBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingBLL _bettingBll;
        private readonly SettledBLL _settledBll;
        private readonly Log2File _log2File;
        private readonly IHostEnvironment _environment;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        #endregion

        #region Constructors

        public YGGController(IHostEnvironment environment,
                             AuthBLL authBLL,
                             ThirdPartyBLL thirdPartyBLL,
                             TpCustInfoBLL tpCustInfoBLL,
                             BettingBLL bettingBLL,
                             SettledBLL settledBLL,
                             Log2File log2File)
        {
            this._authBll = authBLL;
            this._thirdPartyBll = thirdPartyBLL;
            this._tpCustInfoBll = tpCustInfoBLL;
            this._bettingBll = bettingBLL;
            this._settledBll = settledBLL;
            this._log2File = log2File;
            this._environment = environment;

            _thisTp = Tp.YGG;
            _gameType = (int)GameType.YGG_RNG;
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

        [Route("[controller]/playerinfo.json")]
        public PlayerInfoRep PlayerInfo([FromForm] Dictionary<string, string> playerInfoReq)
        {
            var playerInfoRep = new PlayerInfoRep
            {
                code = 1
            };

            if (playerInfoReq.TryGetValue("sessiontoken", out string value))
            {
                try
                {
                    var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(value));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    var authToken = sessionKeyArr[0]; /* 1 Time*/
                    var sessionToken = sessionKeyArr[1];/* Long Time*/
                    if (_authBll.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
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
                            var thirdPartySetting = _thirdPartyBll.GetThirdPartySetting((int)_thisTp);
                            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
                            var playerInfoData = new PlayerInfoData()
                            {
                                balance = tpCustBalance.Balance,
                                playerId = tpCustInfoAuth.TpUserName,
                                organization = thirdPartySetting.TpSetting["org"],
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
                    _log2File.Error($"{JsonConvert.SerializeObject(playerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}\r\n{ex}", "YGG_playerinfo", ex);
                }
            }
            else
            {
                playerInfoRep.code = 1000;
            }

            _log2File.Info($"{JsonConvert.SerializeObject(playerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}", "YGG_playerinfo");
            return playerInfoRep;
        }

        /// <summary>
        /// 下注
        /// </summary>
        /// <param name="wagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/wager.json")]
        public WagerRep Wager([FromForm] Dictionary<string, string> wagerReq)
        {
            WagerRep wagerRep;
            var statusCode = 1;
            var wagerData = new WagerData();

            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100005"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //wagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            if (wagerReq.TryGetValue("sessiontoken", out string value))
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(value));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var sessionToken = sessionKeyArr[1];/* Long Time*/
                var wagerReqObj = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(wagerReq));

                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, wagerReqObj.playerid);
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
                        playerId = wagerReqObj.playerid,
                        organization = wagerReqObj.org,
                        currency = wagerReqObj.currency,
                        homeCurrency = wagerReqObj.currency,
                        applicableBonus = 0
                    };

                    try
                    {
                        if (_bettingBll.PlaceBetRNG_YGG(wagerReqObj, out decimal tpAfterAmount) > 0)
                        {
                            if (wagerReqObj.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, wagerReqObj.playerid, _gameType);
                                wagerData.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                wagerData.balance = tpAfterAmount;
                            }
                            statusCode = 0;
                        }
                        else if (tpAfterAmount < 0)
                        {
                            var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, wagerReqObj.playerid, _gameType);
                            wagerData.balance = tpCustBalance.Balance;
                            statusCode = 1006;
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, wagerReqObj.playerid, _gameType);
                            wagerData.balance = tpCustBalance.Balance;
                            statusCode = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        wagerRep = new WagerRep()
                        {
                            code = 1,
                            data = wagerData
                        };

                        _log2File.Error($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Wager", ex);
                    }
                }
            }
            else
            {
                statusCode = 1000;
            }

            wagerRep = new WagerRep()
            {
                code = statusCode,
                data = wagerData
            };

            _log2File.Info($"{JsonConvert.SerializeObject(wagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Wager");

            return wagerRep;
        }

        /// <summary>
        /// 结算
        /// </summary>
        /// <param name="endWagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/endwager.json")]
        public WagerRep EndWager([FromForm] Dictionary<string, string> endWagerReq)
        {
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
                var payoutDataRngYgg = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(JsonConvert.SerializeObject(endWagerReq));
                var result = _settledBll.PayoutRNG_YGG(payoutDataRngYgg, out decimal tpAfterAmount, SettleFlag.EndGame);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRngYgg.playerid,
                    organization = endWagerReq["org"],
                    currency = payoutDataRngYgg.currency,
                    homeCurrency = payoutDataRngYgg.currency,
                    applicableBonus = 0
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
                        var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };

                _log2File.Info($"{JsonConvert.SerializeObject(endWagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Endwager");
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                _log2File.Error($"{JsonConvert.SerializeObject(endWagerReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Endwager", ex);
            }

            return wagerRep;
        }

        /// <summary>
        /// 取消下注
        /// </summary>
        /// <param name="cancelWagerReq"></param>
        /// <returns></returns>
        [Route("[controller]/cancelwager.json")]
        public CancelwagerRep CancelWager([FromForm] Dictionary<string, string> cancelWagerReq)
        {
            var cancelWagerData = new CancelwagerData();
            var statusCode = 1;

            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_127586"",""reference"":""2101291333560100000"",""subreference"":""w2101291333560100001"",""version"":""3""}";
                //cancelwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            var wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(cancelWagerReq));
            var result = _settledBll.CancelBetRNG_YGG(wagerReq, out decimal tpAfterAmount);

            if (result > 0)
            {
                var tpCustBalance = _environment.IsDevelopment() 
                    ? _tpCustInfoBll.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType) 
                    : _tpCustInfoBll.GetBalanceByHalfSecCache((int)Tp.YGG, wagerReq.playerid, _gameType);

                statusCode = 0;
                cancelWagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = cancelWagerReq["org"],
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpAfterAmount
                };

                if (tpAfterAmount == 0)
                {
                    tpCustBalance = _tpCustInfoBll.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType);
                    cancelWagerData.balance = tpCustBalance.Balance;
                    cancelWagerData.currency = tpCustBalance.TpCurrencyId;
                }
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalance((int)Tp.YGG, wagerReq.playerid, _gameType);

                statusCode = 0;
                cancelWagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = cancelWagerReq["org"],
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpCustBalance.Balance
                };
            }
            var cancelWagerRep = new CancelwagerRep()
            {
                code = statusCode,
                data = cancelWagerData
            };

            _log2File.Info($"{JsonConvert.SerializeObject(cancelWagerReq)}\r\n{JsonConvert.SerializeObject(cancelWagerRep)}", "YGG_Cancelwager");

            return cancelWagerRep;
        }

        /// <summary>
        /// 额外结果
        /// </summary>
        /// <param name="appendWagerResultReq"></param>
        /// <returns></returns>
        [Route("[controller]/appendwagerresult.json")]
        public WagerRep AppendWagerResult([FromForm] Dictionary<string, string> appendWagerResultReq)
        {
            _log2File.Info(JsonConvert.SerializeObject(appendWagerResultReq), "YGG_Appendwagerresult");
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
                var payoutDataRngYgg = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(JsonConvert.SerializeObject(appendWagerResultReq));
                var result = _settledBll.PayoutRNG_YGG(payoutDataRngYgg, out decimal tpAfterAmount, SettleFlag.Running);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRngYgg.playerid,
                    organization = appendWagerResultReq["org"],
                    currency = payoutDataRngYgg.currency,
                    homeCurrency = payoutDataRngYgg.currency,
                    applicableBonus = 0
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
                        var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = _tpCustInfoBll.GetBalance((int)_thisTp, payoutDataRngYgg.playerid, _gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };

                _log2File.Info($"{JsonConvert.SerializeObject(appendWagerResultReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Appendwagerresult");
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                _log2File.Error($"{JsonConvert.SerializeObject(appendWagerResultReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Appendwagerresult", ex);
            }

            return wagerRep;
        }

        /// <summary>
        /// 活动支出
        /// </summary>
        /// <param name="campaignPayoutReq"></param>
        /// <returns></returns>
        [Route("[controller]/campaignpayout.json")]
        public WagerRep CampaignPayout([FromForm] Dictionary<string, string> campaignPayoutReq)
        {
            if (_environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            var wagerRep = new WagerRep
            {
                code = 1
            };

            if (campaignPayoutReq.TryGetValue("sessiontoken", out string value))
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(value));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var sessionToken = sessionKeyArr[1];/* Long Time*/
                var wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(campaignPayoutReq));

                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, wagerReq.playerid);
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
            }
            else
            {
                wagerRep.code = 1000;
            }

            _log2File.Info($"{JsonConvert.SerializeObject(campaignPayoutReq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Campaignpayout");
            return wagerRep;
        }

        /// <summary>
        /// 取得余额
        /// </summary>
        /// <param name="getBalanceReq"></param>
        /// <returns></returns>
        [Route("[controller]/getbalance.json")]
        public GetbalanceRep GetBalance([FromForm] Dictionary<string, string> getBalanceReq)
        {
            var statusCode = 1;
            var getBalanceData = new GetbalanceData();

            if (getBalanceReq.TryGetValue("sessiontoken", out string value))
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(value));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var sessionToken = sessionKeyArr[1];/* Long Time*/
                var balanceReq = JsonConvert.DeserializeObject<GetbalanceReq>(JsonConvert.SerializeObject(getBalanceReq));

                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, balanceReq.playerid);
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
                    var thirdPartySetting = _thirdPartyBll.GetThirdPartySetting((int)_thisTp);
                    var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)_thisTp, balanceReq.playerid, _gameType);

                    getBalanceData = new GetbalanceData()
                    {
                        currency = tpCustBalance.TpCurrencyId,
                        applicableBonus = 0,
                        homeCurrency = tpCustBalance.TpCurrencyId,
                        organization = thirdPartySetting.TpSetting["org"],
                        balance = tpCustBalance.Balance,
                        playerId = balanceReq.playerid,
                        bonus = 0
                    };

                    statusCode = 0;
                }
            }
            else
            {
                statusCode = 1000;
            }

            var getBalanceRep = new GetbalanceRep()
            {
                code = statusCode,
                data = getBalanceData
            };

            _log2File.Info($"{JsonConvert.SerializeObject(getBalanceReq)}\r\n{JsonConvert.SerializeObject(getBalanceRep)}", "YGG_getbalance");
            return getBalanceRep;
        }
    }
}