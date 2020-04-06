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
        private readonly AuthBLL authBLL;
        private readonly ILogger<JokerController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IHostEnvironment environment;
        private readonly Tp thisTp;

        public YGGController(IHostEnvironment environment, ILogger<JokerController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.environment = environment;
            this.thisTp = Tp.YGG;
        }

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

        public class PlayerInfoRep
        {
            public int code { get; set; }
            public PlayerInfoData data { get; set; }
        }

        public class PlayerInfoReq
        {
            public string sessiontoken { get; set; }
        }

        [Route("[controller]/playerinfo.json")]
        public PlayerInfoRep playerinfo([FromForm]Dictionary<string, string> PlayerInfoReq)
        {
            PlayerInfoRep playerInfoRep = new PlayerInfoRep();
            playerInfoRep.code = 1;

            if (PlayerInfoReq.TryGetValue("sessiontoken", out string sessiontoken))
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
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName);
                        PlayerInfoData playerInfoData = new PlayerInfoData();
                        playerInfoRep.data = playerInfoData;
                        playerInfoData.balance = tpCustBalance.Balance;
                        playerInfoData.playerId = tpCustInfoAuth.TpUserName;
                        playerInfoData.organization = thirdPartySetting.TpSetting["org"];
                        playerInfoData.currency = tpCustInfoAuth.TpCurrencyId;
                        playerInfoData.homeCurrency = playerInfoData.currency;
                        playerInfoData.applicableBonus = 0;
                        playerInfoRep.code = 0;
                    }
                }
                else
                {
                    playerInfoRep.code = 1000;
                }
            }
            else
            {
                playerInfoRep.code = 1000;
            }

            log2File.Info($"{JsonConvert.SerializeObject(PlayerInfoReq)}\r\n{JsonConvert.SerializeObject(playerInfoRep)}", "YGG_playerinfo");
            return playerInfoRep;
        }

        public class WagerReq : BettingDataRNG_YGG
        {
            public string org { get; set; }
            public string sessiontoken { get; set; }
            public string service { get; set; }
            public string description { get; set; }
            public string cat1 { get; set; }
            public string cat2 { get; set; }
            public string cat3 { get; set; }
            public string cat4 { get; set; }
            public string cat5 { get; set; }
            public string tag1 { get; set; }
            public string tag2 { get; set; }
            public string tag3 { get; set; }
            public string lang { get; set; }
            public int version { get; set; }
        }
        public class WagerData
        {
            public string organization { get; set; }
            public string playerId { get; set; }
            public decimal balance { get; set; }
            public decimal applicableBonus { get; set; }
            public string currency { get; set; }
            public string homeCurrency { get; set; }
        }
        public class WagerRep
        {
            public int code { get; set; }
            public WagerData data { get; set; }
        }
        [Route("[controller]/wager.json")]
        public WagerRep Wager([FromForm]Dictionary<string, string> wagerReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //wagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            WagerRep wagerRep = new WagerRep();
            wagerRep.code = 1;

            if (wagerReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                log2File.Info(JsonConvert.SerializeObject(wagerReqq), "YGG_Wager");

                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(wagerReqq));

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, wagerReq.playerid);
                if (tpCheck == null)
                {
                    wagerRep.code = 1008;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    wagerRep.code = 1008;
                }
                else if (tpCheck.Locked)
                {
                    wagerRep.code = 1007;
                }
                else
                {
                    WagerData wagerData = new WagerData();
                    wagerRep.data = wagerData;
                    //wagerData.balance = tpCustBalance.Balance;
                    wagerData.playerId = wagerReq.playerid;
                    wagerData.organization = wagerReq.org;
                    wagerData.currency = wagerReq.currency;
                    wagerData.homeCurrency = wagerData.currency;
                    wagerData.applicableBonus = 0;
                    //wagerRep.code = 0;

                    if (bettingBLL.PlaceBetRNG_YGG(wagerReq, out decimal tpAffterAmount) > 0)
                    {
                        wagerData.balance = tpAffterAmount;
                        wagerRep.code = 0;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, wagerReq.playerid);
                        wagerData.balance = tpCustBalance.Balance;
                        wagerRep.code = 1006;
                    }
                }
            }
            else
            {
                wagerRep.code = 1000;
            }

            log2File.Info(JsonConvert.SerializeObject(wagerRep), "YGG_Wager");
            return wagerRep;
        }

        [Route("[controller]/endwager.json")]
        public WagerRep Endwager([FromForm]Dictionary<string, string> endwagerReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""playerid"":""DNova88_156"",""amount"":""0.00"",""bonusprize"":""0.00"",""currency"":""THB"",""tickets"":""1"",""service"":""Mir"",""reference"":""2004051539230100001"",""subreference"":""2004051539230100001"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            WagerRep wagerRep = new WagerRep();
            wagerRep.code = 1;
            try
            {
                SettledDataRNG_YGG settledDataRNG_YGG = JsonConvert.DeserializeObject<SettledDataRNG_YGG>(JsonConvert.SerializeObject(endwagerReqq));

                int result = settledBLL.SettledRNG_YGG(settledDataRNG_YGG, out decimal tpAffterAmount);

                WagerData wagerData = new WagerData();
                wagerRep.data = wagerData;
                wagerData.playerId = settledDataRNG_YGG.playerid;
                wagerData.organization = endwagerReqq["org"];
                wagerData.currency = settledDataRNG_YGG.currency;
                wagerData.homeCurrency = wagerData.currency;
                wagerData.applicableBonus = 0;

                if (result > 0)
                {

                    if (settledDataRNG_YGG.amount > 0)
                    {
                        wagerData.balance = tpAffterAmount;
                        wagerRep.code = 0;
                    }
                    else if (settledDataRNG_YGG.amount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, settledDataRNG_YGG.playerid);
                        wagerData.balance = tpCustBalance.Balance;
                        wagerRep.code = 0;
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, settledDataRNG_YGG.playerid);
                    wagerData.balance = tpCustBalance.Balance;
                    wagerRep.code = 0;
                }
            }
            catch(Exception ex)
            {
                log2File.Info($"{JsonConvert.SerializeObject(endwagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}\r\n{ex}", "YGG_Endwager");
            }
            log2File.Info($"{JsonConvert.SerializeObject(endwagerReqq)}\r\n{JsonConvert.SerializeObject(wagerRep)}", "YGG_Endwager");
            return wagerRep;
        }

        [Route("[controller]/cancelwager.json")]
        public WagerRep Cancelwager([FromForm]Dictionary<string, string> cancelwagerReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            log2File.Info(JsonConvert.SerializeObject(cancelwagerReqq), "YGG_Cancelwager");

            WagerRep wagerRep = new WagerRep();
            wagerRep.code = 1;

            if (cancelwagerReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(cancelwagerReqq));

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

            log2File.Info(JsonConvert.SerializeObject(wagerRep), "YGG_Cancelwager");
            return wagerRep;
        }

        [Route("[controller]/appendwagerresult.json")]
        public WagerRep Appendwagerresult([FromForm]Dictionary<string, string> appendwagerresultReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }
            log2File.Info(JsonConvert.SerializeObject(appendwagerresultReqq), "YGG_Appendwagerresult");

            WagerRep wagerRep = new WagerRep();
            wagerRep.code = 1;

            if (appendwagerresultReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                WagerReq wagerReq = JsonConvert.DeserializeObject<WagerReq>(JsonConvert.SerializeObject(appendwagerresultReqq));

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

            log2File.Info(JsonConvert.SerializeObject(wagerRep), "YGG_Appendwagerresult");
            return wagerRep;
        }

        [Route("[controller]/campaignpayout.json")]
        public WagerRep Campaignpayout([FromForm]Dictionary<string, string> campaignpayoutReqq)
        {
            if (environment.IsDevelopment())
            {
                //var s = @"{""org"":""nova88"",""sessiontoken"":""WyJ3b2pXd0pSTFcwYXQ3Q2o0V0FEczd3IiwieXVxT0JIMmxVeXNDTldnVk9KVlhRIl0="",""playerid"":""DNova88_156"",""amount"":""5.00"",""currency"":""THB"",""service"":""Mir"",""reference"":""2004051357040100001"",""subreference"":""w2004051357040100000"",""description"":""Start Game"",""cat1"":""Casino"",""cat2"":""Slot"",""cat3"":""5reel"",""cat4"":""Jokerizer"",""cat5"":""7301"",""tag1"":""GameName.Jokerizer"",""tag2"":""Model.M1"",""tag3"":""Channel.pc"",""lang"":""zh_hant"",""version"":""4""}";
                //endwagerReqq = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }

            log2File.Info(JsonConvert.SerializeObject(campaignpayoutReqq), "YGG_Campaignpayout");
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

            log2File.Info(JsonConvert.SerializeObject(wagerRep), "YGG_Campaignpayout");
            return wagerRep;
        }

        public class GetbalanceReq
        {
            public string org { get; set; }
            public string playerid { get; set; }
            public string sessiontoken { get; set; }
        }

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

        public class GetbalanceRep
        {
            public int code { get; set; }
            public GetbalanceData data { get; set; }
        }

        [Route("[controller]/getbalance.json")]
        public GetbalanceRep Getbalance([FromForm]Dictionary<string, string> getbalanceReqq)
        {
            GetbalanceRep getbalanceRep = new GetbalanceRep();
            getbalanceRep.code = 1;

            if (getbalanceReqq.TryGetValue("sessiontoken", out string sessiontoken))
            {
                log2File.Info(JsonConvert.SerializeObject(getbalanceReqq), "YGG_getbalance");
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(sessiontoken));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                GetbalanceReq getbalanceReq = JsonConvert.DeserializeObject<GetbalanceReq>(JsonConvert.SerializeObject(getbalanceReqq));

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getbalanceReq.playerid);
                if (tpCheck == null)
                {
                    getbalanceRep.code = 1008;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    getbalanceRep.code = 1008;
                }
                else if (tpCheck.Locked)
                {
                    getbalanceRep.code = 1007;
                }
                else
                {
                    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getbalanceReq.playerid);
                    GetbalanceData getbalanceData = new GetbalanceData();
                    getbalanceRep.data = getbalanceData;
                    getbalanceData.balance = tpCustBalance.Balance;
                    getbalanceData.playerId = getbalanceReq.playerid;
                    getbalanceData.organization = thirdPartySetting.TpSetting["org"];
                    getbalanceData.currency = tpCustBalance.TpCurrencyId;
                    getbalanceData.homeCurrency = getbalanceData.currency;
                    getbalanceData.applicableBonus = 0;
                    getbalanceData.bonus = 0;
                    getbalanceRep.code = 0;
                }
            }
            else
            {
                getbalanceRep.code = 1000;
            }

            log2File.Info(JsonConvert.SerializeObject(getbalanceRep), "YGG_getbalance");
            return getbalanceRep;
        }
    }
}