using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        private readonly AuthBLL authBLL;
        private readonly ILogger<JokerController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;

        private readonly Tp thisTp;
        private readonly int gameType = 0;

        #endregion

        #region Constructors

        public YGGController(IHostEnvironment environment,
                             ILogger<JokerController> logger,
                             AuthBLL authBLL,
                             ThirdPartyBLL thirdPartyBLL,
                             TpCustInfoBLL tpCustInfoBLL,
                             BettingBLL bettingBLL,
                             SettledBLL settledBLL,
                             Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.environment = environment;

            thisTp = Tp.YGG;
            gameType = (int)GameType.YGG_RNG;
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
        public PlayerInfoRep playerinfo([FromForm] Dictionary<string, string> PlayerInfoReq)
        {
            PlayerInfoRep playerInfoRep = new PlayerInfoRep();
            playerInfoRep.code = 1;

            if (PlayerInfoReq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    string authToken = sessionKeyArr[0]; /* 1 Time*/
                    string sessionToken = sessionKeyArr[1];/* Long Time*/
                    if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
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
                            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                            PlayerInfoData playerInfoData = new PlayerInfoData()
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
                    log2File.Error($"{JsonConvert.SerializeObject(PlayerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}\r\n{ex}", "YGG_playerinfo", ex);
                }
            }
            else
            {
                playerInfoRep.code = 1000;
            }

            log2File.Info($"{JsonConvert.SerializeObject(PlayerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}", "YGG_playerinfo");
            return playerInfoRep;
        }

        [Route("[controller]/wager.json")]
        public WagerRep Wager([FromForm] Dictionary<string, string> wagerReqq)
        {
            WagerRep wagerRep = new WagerRep();
            int statusCode = 1;
            WagerData wagerData = new WagerData();

            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100005"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //wagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            if (wagerReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(wagerReqq));

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, wagerReq.playerid);
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
                        if (bettingBLL.PlaceBetRNG_YGG(wagerReq, out decimal tpAffterAmount) > 0)
                        {
                            if (wagerReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, wagerReq.playerid, gameType);
                                wagerData.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                wagerData.balance = tpAffterAmount;
                            }
                            statusCode = 0;
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, wagerReq.playerid, gameType);
                            wagerData.balance = tpCustBalance.Balance;
                            statusCode = 1006;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, wagerReq.playerid, gameType);
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

                        log2File.Error($"{JsonConvert.SerializeObject(wagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Wager", ex);
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

            log2File.Info($"{JsonConvert.SerializeObject(wagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Wager");

            return wagerRep;
        }

        [Route("[controller]/endwager.json")]
        public WagerRep Endwager([FromForm] Dictionary<string, string> endwagerReqq)
        {
            WagerRep wagerRep = new WagerRep();
            int statusCode = 1;
            WagerData wagerData = new WagerData();

            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_156"",""amount"":""0.00"",""bonusprize"":""0.00"",""currency"":""THB"",""tickets"":""1"",""service"":""Mir"",""reference"":""2004051539230100001"",""subreference"":""2004051539230100001"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            try
            {
                PayoutDataRNG_YGG payoutDataRNG_YGG = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(JsonConvert.SerializeObject(endwagerReqq));

                int result = settledBLL.PayoutRNG_YGG(payoutDataRNG_YGG, out decimal tpAffterAmount, SettleFlag.EndGame);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRNG_YGG.playerid,
                    organization = endwagerReqq["org"],
                    currency = payoutDataRNG_YGG.currency,
                    homeCurrency = payoutDataRNG_YGG.currency,
                    applicableBonus = 0
                };

                if (result > 0)
                {

                    if (payoutDataRNG_YGG.amount > 0)
                    {
                        wagerData.balance = tpAffterAmount;
                        statusCode = 0;
                    }
                    else if (payoutDataRNG_YGG.amount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, payoutDataRNG_YGG.playerid, gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, payoutDataRNG_YGG.playerid, gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };


                log2File.Info($"{JsonConvert.SerializeObject(endwagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Endwager");
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                log2File.Error($"{JsonConvert.SerializeObject(endwagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Endwager", ex);
            }

            return wagerRep;
        }

        [Route("[controller]/cancelwager.json")]
        public CancelwagerRep Cancelwager([FromForm] Dictionary<string, string> cancelwagerReqq)
        {
            CancelwagerRep cancelwagerRep = new CancelwagerRep();
            int statusCode = 1;
            CancelwagerData cancelwagerData = new CancelwagerData();

            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_127586"",""reference"":""2101291333560100000"",""subreference"":""w2101291333560100001"",""version"":""3""}";
                //cancelwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(cancelwagerReqq));
            int result = settledBLL.CancelBetRNG_YGG(wagerReq, out decimal tpAffterAmount);

            if (result > 0)
            {
                var tpCustBalance = new TpCustBalance();
                if (environment.IsDevelopment())
                {
                    //Because YGG Testing will Keep Send Request.So Can't Get Cache Data.
                    tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, gameType);
                }
                else
                {
                    tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YGG, wagerReq.playerid, gameType);
                }

                statusCode = 0;
                cancelwagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = cancelwagerReqq["org"],
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpAffterAmount
                };

                if (tpAffterAmount == 0)
                {
                    tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, gameType);
                    cancelwagerData.balance = tpCustBalance.Balance;
                    cancelwagerData.currency = tpCustBalance.TpCurrencyId;
                }
            }
            else if (result == -1)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.YGG, wagerReq.playerid, gameType);

                statusCode = 0;
                cancelwagerData = new CancelwagerData()
                {
                    playerId = wagerReq.playerid,
                    organization = cancelwagerReqq["org"],
                    currency = tpCustBalance.TpCurrencyId,
                    balance = tpCustBalance.Balance
                };
            }
            cancelwagerRep = new CancelwagerRep()
            {
                code = statusCode,
                data = cancelwagerData
            };

            log2File.Info($"{JsonConvert.SerializeObject(cancelwagerReqq)}\r\n{JsonConvert.SerializeObject(cancelwagerRep)}", "YGG_Cancelwager");

            return cancelwagerRep;
        }

        [Route("[controller]/appendwagerresult.json")]
        public WagerRep Appendwagerresult([FromForm] Dictionary<string, string> appendwagerresultReqq)
        {
            WagerRep wagerRep = new WagerRep();
            int statusCode = 1;
            WagerData wagerData = new WagerData();

            if (environment.IsDevelopment())
            {
                /*var s = @"{""org"":""nova88"",""playerid"":""DNova88_156"",""amount"":""7136.45"",""bonusprize"":""0.00"",""currency"":""THB"",""tickets"":""0"",""service"":""Mir"",""reference"":""2004061617230100001"",""subreference"":""2004061617230100000.2004061617240100000"",""description"":""Jackpot"",""isJackpotWin"":""true"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""ShardsJ"",""cat4"":""Holmes and the Stolen Stones"",""cat5"":""7317"",""tag1"":""GameName.Holmes and the Stolen Stones"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                appendwagerresultReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);*/
            }
            log2File.Info(JsonConvert.SerializeObject(appendwagerresultReqq), "YGG_Appendwagerresult");

            try
            {
                PayoutDataRNG_YGG payoutDataRNG_YGG = JsonConvert.DeserializeObject<PayoutDataRNG_YGG>(JsonConvert.SerializeObject(appendwagerresultReqq));

                int result = settledBLL.PayoutRNG_YGG(payoutDataRNG_YGG, out decimal tpAffterAmount, SettleFlag.Running);

                wagerData = new WagerData()
                {
                    playerId = payoutDataRNG_YGG.playerid,
                    organization = appendwagerresultReqq["org"],
                    currency = payoutDataRNG_YGG.currency,
                    homeCurrency = payoutDataRNG_YGG.currency,
                    applicableBonus = 0
                };

                if (result > 0)
                {
                    if (payoutDataRNG_YGG.amount > 0)
                    {
                        wagerData.balance = tpAffterAmount;
                        statusCode = 0;
                    }
                    else if (payoutDataRNG_YGG.amount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, payoutDataRNG_YGG.playerid, gameType);
                        wagerData.balance = tpCustBalance.Balance;
                        statusCode = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, payoutDataRNG_YGG.playerid, gameType);
                    wagerData.balance = tpCustBalance.Balance;
                    statusCode = 0;
                }

                wagerRep = new WagerRep()
                {
                    code = statusCode,
                    data = wagerData
                };

                log2File.Info($"{JsonConvert.SerializeObject(appendwagerresultReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Appendwagerresult");
            }
            catch (Exception ex)
            {
                wagerRep = new WagerRep()
                {
                    code = 1,
                    data = wagerData
                };

                log2File.Error($"{JsonConvert.SerializeObject(appendwagerresultReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Appendwagerresult", ex);
            }

            return wagerRep;
        }

        [Route("[controller]/campaignpayout.json")]
        public WagerRep Campaignpayout([FromForm] Dictionary<string, string> campaignpayoutReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            WagerRep wagerRep = new WagerRep();
            wagerRep.code = 1;

            if (campaignpayoutReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(campaignpayoutReqq));

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, wagerReq.playerid);
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
                else
                {
                    //WagerData wagerData = new WagerData();
                    //wagerRep.data = wagerData;
                    ////wagerData.balance = tpCustBalance.Balance;
                    //wagerData.playerId = wagerReq.playerid;
                    //wagerData.organization = wagerReq.org;
                    //wagerData.currency = wagerReq.currency;
                    //wagerData.homeCurrency = wagerData.currency;
                    //wagerData.applicableBonus = 0;
                    ////wagerRep.code = 0;

                    //if (bettingBLL.PlaceBetRNG_YGG(wagerReq, out decimal tpAffterAmount) > 0)
                    //{
                    //    wagerData.balance = tpAffterAmount;
                    //    wagerRep.code = 0;
                    //}
                    //else if (tpAffterAmount < 0)
                    //{
                    //    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, wagerReq.playerid);
                    //    wagerData.balance = tpCustBalance.Balance;
                    //    wagerRep.code = 1006;
                    //}
                }
            }
            else
            {
                wagerRep.code = 1000;
            }

            log2File.Info($"{JsonConvert.SerializeObject(campaignpayoutReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Campaignpayout");
            return wagerRep;
        }

        [Route("[controller]/getbalance.json")]
        public GetbalanceRep Getbalance([FromForm] Dictionary<string, string> getbalanceReqq)
        {
            GetbalanceRep getbalanceRep = new GetbalanceRep();
            int statusCode = 1;
            GetbalanceData getbalanceData = new GetbalanceData();

            if (getbalanceReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                GetbalanceReq getbalanceReq = JsonConvert.DeserializeObject<GetbalanceReq>(JsonConvert.SerializeObject(getbalanceReqq));

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getbalanceReq.playerid);
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
                    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getbalanceReq.playerid, gameType);

                    getbalanceData = new GetbalanceData()
                    {
                        currency = tpCustBalance.TpCurrencyId,
                        applicableBonus = 0,
                        homeCurrency = tpCustBalance.TpCurrencyId,
                        organization = thirdPartySetting.TpSetting["org"],
                        balance = tpCustBalance.Balance,
                        playerId = getbalanceReq.playerid,
                        bonus = 0

                    };

                    statusCode = 0;
                }
            }
            else
            {
                statusCode = 1000;
            }

            getbalanceRep = new GetbalanceRep()
            {
                code = statusCode,
                data = getbalanceData
            };

            log2File.Info($"{JsonConvert.SerializeObject(getbalanceReqq)}\r\n{JsonConvert.SerializeObject(getbalanceRep)}", "YGG_getbalance");
            return getbalanceRep;
        }
    }
}