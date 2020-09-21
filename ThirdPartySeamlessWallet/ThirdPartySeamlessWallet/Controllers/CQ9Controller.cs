using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    //[Route("api/[controller]")]
    [ApiController]
    public class CQ9Controller : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<CQ9Controller> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly string tpGameName;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public CQ9Controller(
            IHostEnvironment environment,
            ILogger<CQ9Controller> logger,
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            Log2File log2File
        )
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.thisTp = Tp.CQ9;
            gameType = (int)GameType.CQ9_RNG;
            tpGameName = "CQ9";
            this.environment = environment;
        }

        public class GetHeader
        {
            [FromHeader]
            public string wtoken { get; set; }
        }

        #region Request
        public class GetBalanceReq
        {
            public string gamecode { get; set; }
        }
        #endregion

        #region Response
        public class CheckPlayerResp
        {
            public bool data { get; set; }
            public GameResponse status { get; set; }
        }

        public class GetApiResp
        {
            public GetApiResp()
            {
                data = new BalanceData();
                status = new GameResponse();
            }
            public BalanceData data { get; set; }
            public GameResponse status { get; set; }
        }

        public class BalanceData
        {
            public decimal balance { get; set; }
            public string currency { get; set; }
        }

        public class GameResponse
        {
            public string code { get; set; }
            public string message { get; set; }
            public string datetime { get; set; }
        }

        #endregion

        [HttpGet]
        [Route("[controller]/player/check/{account}")]
        public CheckPlayerResp CheckPlayer(string account, [FromQuery] GetHeader getHeader)
        {
            string logName = $"{tpGameName}_CheckPlayer";
            log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getHeader)}", logName);
            CheckPlayerResp checkPlayerResp = new CheckPlayerResp();
            checkPlayerResp.status = new GameResponse();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["wtoken"] != HttpUtility.UrlDecode(getHeader.wtoken))
            {
                checkPlayerResp.status.code = "1100";
                checkPlayerResp.status.message = "Error : wtoken";
                checkPlayerResp.status.datetime = getCurrentGameTime();
                checkPlayerResp.data = false;
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, account);
            if (tpCheck == null || tpCheck.Locked)
            {
                checkPlayerResp.status.code = "0";
                checkPlayerResp.status.message = "Success";
                checkPlayerResp.status.datetime = getCurrentGameTime();
                checkPlayerResp.data = false;
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            checkPlayerResp.status.code = "0";
            checkPlayerResp.status.message = "Success";
            checkPlayerResp.status.datetime = getCurrentGameTime();
            checkPlayerResp.data = true;

            log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
            return checkPlayerResp;
        }

        [HttpGet]
        [Route("[controller]/transaction/balance/{account}")]
        public GetApiResp Balance(string account, [FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getBalanceReq)
        {
            string logName = $"{tpGameName}_Balance";
            log2File.Info($"account={account}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getBalanceReq)}", logName);

            GetApiResp getBalanceResp = new GetApiResp();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["wtoken"] != wtoken)
            {
                getBalanceResp.status.code = "1100";
                getBalanceResp.status.message = "Error : wtoken";
                getBalanceResp.status.datetime = getCurrentGameTime();
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, account);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBalanceResp.status.code = "1006";
                getBalanceResp.status.message = "Error : account not found";
                getBalanceResp.status.datetime = getCurrentGameTime();
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, account, gameType);
            getBalanceResp.status.code = "0";
            getBalanceResp.status.message = "Success";
            getBalanceResp.status.datetime = getCurrentGameTime();
            getBalanceResp.data.balance = tpCustBalance.Balance;
            getBalanceResp.data.currency = tpCustBalance.TpCurrencyId;

            log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
            return getBalanceResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/bet")]
        public GetApiResp Bet([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getBetReq)
        {
            string logName = $"{tpGameName}_Bet";
            var json = JsonConvert.SerializeObject(getBetReq);
            var betReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);
            log2File.Info($"wtoken={wtoken}\r\n{betReq}", logName);

            GetApiResp getBetResp = new GetApiResp();
            getBetResp.status.code = "1100";
            getBetResp.status.message = "Error";
            getBetResp.status.datetime = getCurrentGameTime();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["wtoken"] != wtoken)
            {
                getBetResp.status.code = "1100";
                getBetResp.status.message = "Error : wtoken";
                getBetResp.status.datetime = getCurrentGameTime();
                log2File.Info($"{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check request param
            if (betReq.amount <= 0)
            {
                // 當amount不得為負值為負值時則反回錯誤編碼1003
                getBetResp.status.code = "1003";
                getBetResp.status.message = "Error : insufficient amount";
                getBetResp.status.datetime = getCurrentGameTime();
                log2File.Info($"{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            DateTime dtFormat;
            if (!DateTime.TryParse(betReq.eventTime, out dtFormat))
            {
                // 當eventTime格式錯誤時回傳 1004
                getBetResp.status.code = "1004";
                getBetResp.status.message = "Error : invalid eventTime";
                getBetResp.status.datetime = getCurrentGameTime();
                log2File.Info($"{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check session
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, betReq.account);
            if (tpCheck == null || tpCheck.Locked || tpCheck.Token != HttpUtility.UrlDecode(betReq.session))
            {
                getBetResp.status.code = "1006";
                getBetResp.status.message = "Error : account not found";
                getBetResp.status.datetime = getCurrentGameTime();
                log2File.Info($"{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            try
            {
                int result = bettingBLL.PlaceBetRNG_CQ9(betReq, out decimal tpAffterAmount);
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.account);
                if (result > 0)
                {
                    getBetResp.status.code = "0";
                    getBetResp.status.message = "Success";
                    getBetResp.status.datetime = getCurrentGameTime();
                    getBetResp.data.balance = tpAffterAmount;
                    getBetResp.data.currency = tpCustBalance.TpCurrencyId;
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    getBetResp.status.code = "2009";
                    getBetResp.status.message = "Error : mtcode exist";
                    getBetResp.status.datetime = getCurrentGameTime();
                    getBetResp.data.balance = tpAffterAmount;
                    getBetResp.data.currency = tpCustBalance.TpCurrencyId;
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    getBetResp.status.code = "1005";
                    getBetResp.status.message = "Error : insufficient amount";
                    getBetResp.status.datetime = getCurrentGameTime();
                    getBetResp.data.balance = tpAffterAmount;
                    getBetResp.data.currency = tpCustBalance.TpCurrencyId;
                }

                log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
            }
            catch (Exception ex)
            {
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex}", logName);
            }

            return getBetResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/endround")]
        public GetApiResp EndRound([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getEndRoundReq)
        {
            string logName = $"{tpGameName}_EndRound";
            var json = JsonConvert.SerializeObject(getEndRoundReq);
            var endRoundReq = JsonConvert.DeserializeObject<EndRoundDataRNG_CQ9>(json);
            log2File.Info($"wtoken={wtoken}\r\n{endRoundReq}", logName);

            GetApiResp getEndRoundResp = new GetApiResp();

            getEndRoundResp.status.code = "1100";
            getEndRoundResp.status.message = "Error";
            getEndRoundResp.status.datetime = getCurrentGameTime();

            foreach (var item in endRoundReq.data)
            {
                if (item.amount < 0)
                {
                    // amount不得為負值為負值時則反回錯誤編碼1003
                    getEndRoundResp.status.code = "1003";
                    getEndRoundResp.status.message = "Error : insufficient amount";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                    log2File.Info($"{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }
                DateTime dtFormat;
                if (!DateTime.TryParse(item.eventtime, out dtFormat))
                {
                    // 當eventTime格式錯誤時回傳 1004
                    getEndRoundResp.status.code = "1004";
                    getEndRoundResp.status.message = "Error : invalid eventtime";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                    log2File.Info($"{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }
            }
            /*
            try
            {
                var result = settledBLL.PayoutRNG_CQ9(endRoundReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, endRoundReq.account, gameType);
                    getEndRoundResp.status.code = "0";
                    getEndRoundResp.status.message = "Success";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                    getEndRoundResp.data.balance = tpAffterAmount;
                    getEndRoundResp.data.currency = tpCustBalance.TpCurrencyId;
                }
                else if (result == -1)
                {
                    // mtcode重複時則反回錯誤編碼 2009
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, endRoundReq.account, gameType);
                    getEndRoundResp.status.code = "2009";
                    getEndRoundResp.status.message = "Error : mtcode";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, endRoundReq.account, gameType);
                    getEndRoundResp.status.code = "1100";
                    getEndRoundResp.status.message = "Error";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}\r\n{ex}", logName);
                return getEndRoundResp;
            }
            */
            log2File.Info($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
            return getEndRoundResp;
        }

        //[HttpGet]
        //[Route("[controller]/transaction/record/{mtcode}")]
        //public GetApiResp Record(string mtcode, [FromHeader(Name = "wtoken")] string wtoken)
        //{

        //}

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
        }
    }
}