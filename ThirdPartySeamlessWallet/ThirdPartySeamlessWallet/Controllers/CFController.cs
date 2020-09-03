using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
    [Route("[controller]/[action]")]
    public class CFController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<CFController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public CFController(IHostEnvironment environment, ILogger<CFController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.thisTp = Tp.CF;
            gameType = (int)GameType.CF_Rooster;
            this.environment = environment;
        }

        public class GetBalanceReq
        {
            public string api_key { get; set; }
            public string agent_code { get; set; }
            public string login_id { get; set; }
        }

        [XmlRoot(ElementName = "get_balance")]
        public class GetBalanceResp
        {
            [XmlElement(ElementName = "status_code")]
            public string status_code { get; set; }

            [XmlElement(ElementName = "status_text")]
            public string status_text { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }
        }

        public class GetBetReq
        {
            public string api_key { get; set; }
            public string agent_code { get; set; }
            public long ticket_id { get; set; }
            public string login_id { get; set; }
            public string arena_code { get; set; }
            public string match_no { get; set; }
            public DateTime match_date { get; set; }
            public int fight_no { get; set; }
            public DateTime fight_datetime { get; set; }
            public string bet_on { get; set; }
            public decimal odds_given { get; set; }
            public int stake { get; set; }
            public decimal stake_money { get; set; }
            public DateTime created_datetime { get; set; }
        }

        [XmlRoot(ElementName = "bet")]
        public class GetBetResp
        {
            [XmlElement(ElementName = "status_code")]
            public string status_code { get; set; }

            [XmlElement(ElementName = "status_text")]
            public string status_text { get; set; }

            [XmlElement(ElementName = "ref_id")]
            public long ref_id { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }
        }

        public class GetCancelBetReq
        {
            public string api_key { get; set; }
            public string agent_code { get; set; }
            public long ticket_id { get; set; }
        }

        [XmlRoot(ElementName = "cancel_bet")]
        public class GetCancelBetResp
        {
            [XmlElement(ElementName = "status_code")]
            public string status_code { get; set; }

            [XmlElement(ElementName = "status_text")]
            public string status_text { get; set; }
        }



        [HttpPost]
        [Produces("application/xml")]
        public GetBalanceResp get_balance([FromForm] GetBalanceReq getBalanceReq)
        {
            //logger.LogCritical(JsonConvert.SerializeObject(getBalanceReq));

            GetBalanceResp getBalanceResp = new GetBalanceResp();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["api_key"] != getBalanceReq.api_key)
            {
                getBalanceResp.status_code = "61.01";
                getBalanceResp.status_text = "error : api key not found";
                return getBalanceResp;
            }

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.login_id);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBalanceResp.status_code = "99";
                getBalanceResp.status_text = "error : login id not found";
                return getBalanceResp;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.login_id, gameType);
            getBalanceResp.status_code = "00";
            getBalanceResp.status_text = "OK";
            getBalanceResp.balance = tpCustBalance.Balance;
            return getBalanceResp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public GetBetResp bet([FromForm] Dictionary<string, string> getBetReq)
        {
            var json = JsonConvert.SerializeObject(getBetReq);
            var betReq = JsonConvert.DeserializeObject<BettingData_CF>(json);
            log2File.Info(json, $"CF_Bet");
            //logger.LogCritical(json);

            GetBetResp getBetResp = new GetBetResp();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["api_key"] != betReq.api_key)
            {
                getBetResp.status_code = "61.01";
                getBetResp.status_text = "error : api key not found";
                return getBetResp;
            }

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, betReq.login_id);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBetResp.status_code = "99";
                getBetResp.status_text = "error : player suspended";
                return getBetResp;
            }

            try
            {
                // check bet amount
                if (environment.IsDevelopment() && betReq.stake == 44m)
                {
                    System.Threading.Thread.Sleep(60000);
                }

                int result = bettingBLL.PlaceBet_CF(betReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getBetResp.status_code = "00";
                    getBetResp.status_text = "OK";
                    getBetResp.ref_id = betReq.ticket_id;
                    getBetResp.balance = tpAffterAmount;
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.login_id);
                    getBetResp.status_code = "00";
                    getBetResp.status_text = "OK";
                    getBetResp.ref_id = betReq.ticket_id;
                    getBetResp.balance = tpAffterAmount;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, betReq.login_id);
                    getBetResp.status_code = "88";
                    getBetResp.status_text = "error : insufficient fund";
                }

                log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(getBetResp)}", $"CF_Bet");
            }
            catch (Exception ex)
            {
                getBetResp.status_code = "500";
                getBetResp.status_text = "system busy";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex}", $"CF_Bet");
                throw ex;
            }

            return getBetResp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public GetCancelBetResp cancel_bet([FromForm] Dictionary<string, string> getCancelBetReq)
        {
            var json = JsonConvert.SerializeObject(getCancelBetReq);
            var cancelBetReq = JsonConvert.DeserializeObject<CancelBetData_CF>(json);
            log2File.Info(json, $"CF_Cancel_Bet");

            GetCancelBetResp getCancelBetResp = new GetCancelBetResp();

            // check key
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (thirdPartySetting.TpSetting["api_key"] != cancelBetReq.api_key)
            {
                getCancelBetResp.status_code = "61.01";
                getCancelBetResp.status_text = "error : api key not found";
                return getCancelBetResp;
            }

            try
            {
                int result = bettingBLL.CancelBet_CF(cancelBetReq);
            }
            catch (Exception ex)
            {
                getCancelBetResp.status_code = "500";
                getCancelBetResp.status_text = "system busy";
                log2File.Error($"{json}\r\n{JsonConvert.SerializeObject(getCancelBetResp)}\r\n{ex}", $"CF_Cancel_Bet");
                throw ex;
            }

            log2File.Info(JsonConvert.SerializeObject(getCancelBetReq), $"CF_Cancel_Bet");

            getCancelBetResp.status_code = "00";
            getCancelBetResp.status_text = "OK";
            return getCancelBetResp;
        }
    }
}
