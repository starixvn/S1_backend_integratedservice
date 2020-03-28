using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class YLController : ControllerBase
    {
        private readonly ILogger<YLController> logger;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public YLController(ILogger<YLController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.logger = logger;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
        }

        public class Req
        {
            public string key { get; set; }
            public string message { get; set; }
        }
        public class Resp
        {
            public string status { get; set; }
            public string msg { get; set; }
            public string balance { get; set; }
        }

        public class GetBalanceReq
        {
            public string action { get; set; }
            public string userId { get; set; }
        }

        [Route("[controller]/[action]")]
        [HttpPost]
        public Resp GetBalance([FromForm]Req req)
        {
            logger.LogCritical(JsonConvert.SerializeObject(req));
            GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            Resp resp = new Resp();

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.YL, getBalanceReq.userId);
            if (tpCheck == null)
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }
            else if (tpCheck.Locked)
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, getBalanceReq.userId);

            if (tpCustBalance != null)
            {
                resp.balance = tpCustBalance.Balance.ToString("0.##");
                resp.status = "200";
            }
            else
            {
                resp.status = "500";
                resp.msg = "system busy";
            }

            return resp;
        }

        public class BetReq
        {
            public string acton { get; set; }
            public string userId { get; set; }
            public string extension1 { get; set; }
            public string gameNumier { get; set; }
            public int gameId { get; set; }
            public int chair { get; set; }
            public string currency { get; set; }
            public string txId { get; set; }
            public string roomId { get; set; }
            public int room { get; set; }
            public string ietTransTime { get; set; }
            public decimal ietAmount { get; set; }
        }

        [Route("[controller]/[action]")]
        [HttpPost]
        public Resp Bet([FromForm]Req req)
        {
            logger.LogCritical(JsonConvert.SerializeObject(req));
            BetReq betReq = JsonConvert.DeserializeObject<BetReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            Resp resp = new Resp();

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.YL, betReq.userId);
            if (tpCheck == null)
            {
                resp.status = "500";
                resp.msg = "code:9003";
                return resp;
            }
            else if (tpCheck.Locked)
            {
                resp.status = "500";
                resp.msg = "code:9003";
                return resp;
            }

            logger.LogCritical(JsonConvert.SerializeObject(resp));
            return resp;
        }
    }
}