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
using Newtonsoft.Json.Linq;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
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

        public class Payload
        {
            public string currency { get; set; }
            public string account { get; set; }
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
            private decimal _balance = 0m;
            public decimal balance
            {
                get
                {
                    decimal.TryParse(_balance.ToString("#.0000"), out _balance);
                    return _balance;
                }
                set
                {
                    _balance = value;
                }
            }
            public string currency { get; set; }
        }

        public class GameResponse
        {
            public string code { get; set; }
            public string message { get; set; }
            public string datetime { get; set; }
        }

        public class GetRecordResp
        {
            public GetRecordResp()
            {
                data = new RecordData();
                status = new GameResponse();
            }
            public RecordData data { get; set; }
            public GameResponse status { get; set; }
        }

        public class RecordData
        {
            public RecordData()
            {
                target = new Target();
                status = new DetailStatus();
            }
            public string _id { get; set; }
            public string action { get; set; }
            public Target target { get; set; }
            public DetailStatus status { get; set; }
            public decimal before { get; set; }
            private decimal _balance = 0m;
            public decimal balance
            {
                get
                {
                    decimal.TryParse(_balance.ToString("#.0000"), out _balance);
                    return _balance;
                }
                set
                {
                    _balance = value;
                }
            }
            public string currency { get; set; }
            [JsonProperty("event")]
            public List<RecordEvent> Event { get; set; }
        }

        public class Target
        {
            public string account { get; set; }
        }

        public class DetailStatus
        {
            public string createtime { get; set; }
            public string endtime { get; set; }
            public string status { get; set; }
            public string message { get; set; }
        }

        public class RecordEvent
        {
            public string mtcode { get; set; }
            public decimal amount { get; set; }
            public string eventtime { get; set; }
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

            // check param
            if (getHeader.wtoken == null)
            {
                checkPlayerResp.status.code = "1003";
                checkPlayerResp.status.message = "Error : param invalid";
                checkPlayerResp.status.datetime = getCurrentGameTime();
                checkPlayerResp.data = false;
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            // check key
            var tokenCurrency = getTokenCurrency(HttpUtility.UrlDecode(getHeader.wtoken));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != HttpUtility.UrlDecode(getHeader.wtoken))
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
            getBalanceResp.status.datetime = getCurrentGameTime();

            // check param
            if (wtoken == null)
            {
                getBalanceResp.status.code = "1003";
                getBalanceResp.status.message = "Error : param invalid";
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                getBalanceResp.status.code = "1100";
                getBalanceResp.status.message = "Error : wtoken";
                log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, account);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBalanceResp.status.code = "1006";
                getBalanceResp.status.message = "Error : account not found";
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
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var betReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetApiResp getBetResp = new GetApiResp();
            getBetResp.status.code = "1100";
            getBetResp.status.message = "Error";
            getBetResp.status.datetime = getCurrentGameTime();

            if (betReq.amount <= 0)
            {
                // 當amount不得為負值為負值時則反回錯誤編碼1003
                getBetResp.status.code = "1003";
                getBetResp.status.message = "Error : insufficient amount";
                log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check param
            if (wtoken == null || betReq.account == null || betReq.eventTime == null || betReq.gamecode == null || betReq.gamehall == null || betReq.roundid == null || betReq.mtcode == null)
            {
                getBetResp.status.code = "1003";
                getBetResp.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            long chkEventTimeParse = checkTimeParse(betReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                getBetResp.status.code = "1004";
                getBetResp.status.message = "Error : invalid eventTime";
                log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }
            betReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                getBetResp.status.code = "1100";
                getBetResp.status.message = "Error : wtoken";
                log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check session
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, betReq.account);
            if (tpCheck == null || tpCheck.Locked || tpCheck.Token != HttpUtility.UrlDecode(betReq.session))
            {
                getBetResp.status.code = "1006";
                getBetResp.status.message = "Error : account not found";
                log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            try
            {
                int result = bettingBLL.PlaceBetRNG_CQ9(betReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.account); /*Just for TpCurrencyId*/
                    getBetResp.data.balance = tpAffterAmount;
                    getBetResp.data.currency = tpCustBalance.TpCurrencyId;
                    getBetResp.status.code = "0";
                    getBetResp.status.message = "Success";
                    getBetResp.status.datetime = getCurrentGameTime();
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    getBetResp.status.code = "2009";
                    getBetResp.status.message = "Error : mtcode exist";
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    getBetResp.status.code = "1005";
                    getBetResp.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex}", logName);
                return getBetResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
            return getBetResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/endround")]
        public GetApiResp EndRound([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getEndRoundReq)
        {
            var result = 0;
            string logName = $"{tpGameName}_EndRound";
            var json = JsonConvert.SerializeObject(getEndRoundReq);
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var endRoundReq = JsonConvert.DeserializeObject<EndRoundDataRNG_CQ9>(json);

            GetApiResp getEndRoundResp = new GetApiResp();
            getEndRoundResp.status.code = "1100";
            getEndRoundResp.status.message = "Error";
            getEndRoundResp.status.datetime = getCurrentGameTime();

            // check param
            if (wtoken == null || endRoundReq.account == null || endRoundReq.gamecode == null || endRoundReq.gamehall == null || endRoundReq.roundid == null || endRoundReq.data == null || endRoundReq.createTime == null)
            {
                getEndRoundResp.status.code = "1003";
                getEndRoundResp.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(endRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                return getEndRoundResp;
            }
            try
            {
                // check key
                var tokenCurrency = getTokenCurrency(wtoken);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
                {
                    getEndRoundResp.status.code = "1100";
                    getEndRoundResp.status.message = "Error : wtoken";
                    log2File.Info($"{JsonConvert.SerializeObject(endRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }
                // check item param
                foreach (var item in endRoundReq.detail)
                {
                    if (item.amount < 0)
                    {
                        // amount不得為負值為負值時則反回錯誤編碼1003
                        getEndRoundResp.status.code = "1003";
                        getEndRoundResp.status.message = "Error : insufficient amount";
                        log2File.Info($"{JsonConvert.SerializeObject(item)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                        return getEndRoundResp;
                    }

                    long chkEventTimeParse = checkTimeParse(item.eventtime);
                    if (chkEventTimeParse == 0)
                    {
                        // 當eventTime格式錯誤時回傳 1004
                        getEndRoundResp.status.code = "1004";
                        getEndRoundResp.status.message = "Error : invalid eventTime";
                        log2File.Info($"{JsonConvert.SerializeObject(item)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                        return getEndRoundResp;
                    }
                    item.eventUnix = chkEventTimeParse;
                }


                long chkCreateTimeParse = checkTimeParse(endRoundReq.createTime);
                endRoundReq.createUnix = chkCreateTimeParse;

                result = settledBLL.PayoutRNG_CQ9(endRoundReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, endRoundReq.account, gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    getEndRoundResp.data.balance = tpAffterAmount;
                    getEndRoundResp.data.currency = tokenCurrency;
                    getEndRoundResp.status.code = "0";
                    getEndRoundResp.status.message = "Success";
                    getEndRoundResp.status.datetime = getCurrentGameTime();
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    getEndRoundResp.status.code = "2009";
                    getEndRoundResp.status.message = "Error : mtcode exist";
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    getEndRoundResp.status.code = "1006";
                    getEndRoundResp.status.message = "Error : account not found";
                }
                else
                {
                    // 1100 對方重送資料
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}\r\n{ex}", logName);
                return getEndRoundResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
            return getEndRoundResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/refund")]
        public GetApiResp Refund([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getRefundReq)
        {
            string logName = $"{tpGameName}_Refund";
            var json = JsonConvert.SerializeObject(getRefundReq);
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var refundReq = JsonConvert.DeserializeObject<RefundDataRNG_CQ9>(json);

            GetApiResp getRefundResp = new GetApiResp();
            getRefundResp.status.code = "1100";
            getRefundResp.status.message = "Error";
            getRefundResp.status.datetime = getCurrentGameTime();

            // check param
            if (wtoken == null || refundReq.mtcode == null)
            {
                getRefundResp.status.code = "1003";
                getRefundResp.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                return getRefundResp;
            }

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                getRefundResp.status.code = "1100";
                getRefundResp.status.message = "Error : wtoken";
                log2File.Info($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                return getRefundResp;
            }

            try
            {
                var result = settledBLL.RefundRNG_CQ9(refundReq, out decimal tpAffterAmount, out string account);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, account, gameType);
                    getRefundResp.status.code = "0";
                    getRefundResp.status.message = "Success";
                    getRefundResp.status.datetime = getCurrentGameTime();
                    getRefundResp.data.balance = tpCustBalance.Balance;
                    getRefundResp.data.currency = tpCustBalance.TpCurrencyId;
                }
                else if (result == -2)
                {
                    // 當refund未查詢到紀錄時回傳該錯誤編碼1014
                    getRefundResp.status.code = "1014";
                    getRefundResp.status.message = "Error : record not found";
                }
                else if (result == -3)
                {
                    // 當該mtcode已經被refund時回傳該錯誤編碼 1015
                    getRefundResp.status.code = "1015";
                    getRefundResp.status.message = "Error : refunded";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}\r\n{ex}", logName);
                return getRefundResp;
            }
            log2File.Info($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
            return getRefundResp;
        }

        [HttpGet]
        [Route("[controller]/transaction/record/{mtcode}")]
        public GetRecordResp Record(string mtcode, [FromHeader(Name = "wtoken")] string wtoken)
        {
            string logName = $"{tpGameName}_Record";
            log2File.Info($"mtcode={mtcode}\r\nwtoken={wtoken}", logName);

            GetRecordResp getRecordResp = new GetRecordResp();
            getRecordResp.status.code = "1100";
            getRecordResp.status.message = "Error";
            getRecordResp.status.datetime = getCurrentGameTime();

            if (wtoken == null)
            {
                getRecordResp.status.code = "1003";
                getRecordResp.status.message = "Error : param invalid";
                log2File.Info($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                getRecordResp.status.code = "1100";
                getRecordResp.status.message = "Error : wtoken";
                log2File.Info($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }
            string[] sType = mtcode.Split("-");
            if (sType.Count() == 1)
            {
                getRecordResp.status.code = "1014";
                getRecordResp.status.message = "Error : mtcode format invalid";
                log2File.Info($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }

            var data = settledBLL.GetRecordRNG_CQ9(mtcode);
            if (data.Count() == 0)
            {
                // 當未查詢到紀錄時回傳該錯誤編碼1014
                getRecordResp.data = null;
                getRecordResp.status.code = "1014";
                getRecordResp.status.message = "Error : record not found";
                getRecordResp.status.datetime = getCurrentGameTime();
            }
            else
            {
                getRecordResp.data._id = mtcode;
                getRecordResp.data.action = data.ElementAt(0).Actions;
                getRecordResp.data.target.account = data.ElementAt(0).account;
                getRecordResp.data.status.createtime = data.ElementAt(0).CreateFormat;
                getRecordResp.data.status.endtime = data.ElementAt(0).UpdateFormat;
                getRecordResp.data.status.status = (data.ElementAt(0).TicketStatus == (int)TicketStatus.Refund) ? "refund" : "success";
                getRecordResp.data.status.message = "success";
                getRecordResp.data.currency = data.ElementAt(0).currency;
                getRecordResp.data.before = data.First().Before;
                getRecordResp.data.balance = data.Last().Balance;
                getRecordResp.data.Event = new List<RecordEvent>();

                foreach (var item in data)
                {
                    RecordEvent refRecordEvent = new RecordEvent();
                    refRecordEvent.mtcode = item.mtcode;
                    refRecordEvent.amount = Math.Abs(item.amount);
                    refRecordEvent.eventtime = item.eventFormat;
                    getRecordResp.data.Event.Add(refRecordEvent);
                }

                getRecordResp.status.code = "0";
                getRecordResp.status.message = "Success";
                getRecordResp.status.datetime = getCurrentGameTime();
            }

            log2File.Info($"mtcode={mtcode}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
            return getRecordResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/debit")]
        public GetApiResp Debit([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getDebitReq)
        {
            string logName = $"{tpGameName}_Debit";
            var json = JsonConvert.SerializeObject(getDebitReq);
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var debitReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = getCurrentGameTime();

            // 當為負值時則反回錯誤編碼1003
            if (debitReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || debitReq.account == null || debitReq.eventTime == null || debitReq.gamecode == null || debitReq.gamehall == null || debitReq.roundid == null || debitReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = checkTimeParse(debitReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            debitReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                int result = settledBLL.DebitRNG_CQ9(debitReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, debitReq.account); /*Just for TpCurrencyId*/
                    apiResponse.data.balance = tpAffterAmount;
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = getCurrentGameTime();
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    apiResponse.status.code = "2009";
                    apiResponse.status.message = "Error : mtcode exist";
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    apiResponse.status.code = "1005";
                    apiResponse.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex}", logName);
                return apiResponse;
            }
            log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/credit")]
        public GetApiResp Credit([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getCreditReq)
        {
            string logName = $"{tpGameName}_Credit";
            var json = JsonConvert.SerializeObject(getCreditReq);
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var creditReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = getCurrentGameTime();

            // 當為負值時則反回錯誤編碼1003
            if (creditReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || creditReq.account == null || creditReq.eventTime == null || creditReq.gamecode == null || creditReq.gamehall == null || creditReq.roundid == null || creditReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = checkTimeParse(creditReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            creditReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                log2File.Info($"{JsonConvert.SerializeObject(getCreditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                var result = settledBLL.CreditRNG_CQ9(creditReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, creditReq.account);/*Just for TpCurrencyId*/
                    apiResponse.data.balance = tpAffterAmount;
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = getCurrentGameTime();
                }
                else if (result == -1)
                {
                    // 當重複時則反回錯誤編碼 2009
                    apiResponse.status.code = "2009";
                    apiResponse.status.message = "Error : mtcode exist";
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex}", logName);
                return apiResponse;
            }
            log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/user/payoff")]
        public GetApiResp Payoff([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getPayoffReq)
        {
            string logName = $"{tpGameName}_Payoff";
            var json = JsonConvert.SerializeObject(getPayoffReq);
            log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var payoffReq = JsonConvert.DeserializeObject<PayoffDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = getCurrentGameTime();

            // 當為負值時則反回錯誤編碼1003
            if (payoffReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || payoffReq.account == null || payoffReq.eventTime == null || payoffReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = checkTimeParse(payoffReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            payoffReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = getTokenCurrency(wtoken);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting[$"API_Token_{tokenCurrency}"] != wtoken)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                var result = settledBLL.RewardRNG_CQ9(payoffReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, payoffReq.account);/*Just for TpCurrencyId*/
                    apiResponse.data.balance = tpAffterAmount;
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = getCurrentGameTime();
                }
                else if (result == -1)
                {
                    // 當重複時則反回錯誤編碼 2009
                    apiResponse.status.code = "2009";
                    apiResponse.status.message = "Error : mtcode exist";
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex}", logName);
                return apiResponse;
            }
            log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [HttpGet]
        [Route("[controller]/{path1?}/{path2?}/{path3?}")]
        public GetApiResp NotFound([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getReq)
        {
            string logName = $"{tpGameName}_NotFound";
            var json = JsonConvert.SerializeObject(getReq);
            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1002";
            apiResponse.status.message = "Error: 404";
            apiResponse.status.datetime = getCurrentGameTime();
            log2File.Error($"wtoken={wtoken}\r\n{json}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        private long checkTimeParse(string time)
        {
            DateTime dtFormat;
            if (!DateTime.TryParse(time, out dtFormat))
            {
                return 0;
            }
            else
            {
                string[] chkDt = time.Split("-");
                int cntDt = chkDt.Count();
                if (cntDt != 4 || chkDt[3] != "04:00")
                {
                    return 0;
                }
            }
            return new DateTimeOffset(dtFormat).ToUnixTimeSeconds();
        }

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
        }

        private string getTokenCurrency(string token)
        {
            var parts = token.Split('.');
            var header = parts[0];
            var payload = parts[1];
            byte[] crypto = Base64UrlDecode(parts[2]);
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var payloadData = JObject.Parse(payloadJson);
            var tokenData = JsonConvert.DeserializeObject<Payload>(payloadJson);
            if (tokenData.account.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            {
                tokenData.currency = "TEST";
            }
            return tokenData.currency;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }
}