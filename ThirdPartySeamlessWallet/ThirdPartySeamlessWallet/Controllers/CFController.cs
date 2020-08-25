using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
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
        private readonly int gameType = 0;

        public CFController(ILogger<CFController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            gameType = (int)GameType.CF_Rooster;
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

            //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.CF, getBalanceReq.login_id, gameType);
            getBalanceResp.status_code = "00";
            getBalanceResp.balance = 5000;

            return getBalanceResp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public GetBetResp bet([FromForm] Dictionary<string, string> getBetReq)
        {
            log2File.Info(JsonConvert.SerializeObject(getBetReq), $"CF_Bet");
            //logger.LogCritical(JsonConvert.SerializeObject(getBetReq));

            GetBetResp getBetResp = new GetBetResp();
            getBetResp.status_code = "00";

            return getBetResp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public GetCancelBetResp cancel_bet([FromForm] Dictionary<string, string> getCancelBetReq)
        {
            log2File.Info(JsonConvert.SerializeObject(getCancelBetReq), $"CF_Cancel_Bet");
            //logger.LogCritical(JsonConvert.SerializeObject(getCancelBetReq));

            GetCancelBetResp getCancelBetResp = new GetCancelBetResp();
            getCancelBetResp.status_code = "00";

            return getCancelBetResp;
        }
    }
}
