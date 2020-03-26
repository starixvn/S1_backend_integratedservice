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
        public class GetBalanceResp
        {
            public string status { get; set; }
            public string balance { get; set; }
        }

        public class GetBalanceReq
        {
            public string action { get; set; }
            public string userId { get; set; }
        }

        [Route("[controller]/[action]")]
        [HttpPost]
        public GetBalanceResp GetBalance([FromForm]Req req)
        {
            logger.LogCritical(JsonConvert.SerializeObject(req));
            GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            GetBalanceResp getBalanceResp = new GetBalanceResp();

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                getBalanceResp.status = "500";
                return getBalanceResp;
            }
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.GPI, getBalanceReq.userId);
            if (tpCheck == null)
            {
                getBalanceResp.status = "500";
                return getBalanceResp;
            }
            else if (tpCheck.Locked)
            {
                getBalanceResp.status = "500";
                return getBalanceResp;
            }
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, getBalanceReq.userId);

            if (tpCustBalance != null)
            {
                getBalanceResp.balance = tpCustBalance.Balance.ToString("0.##");
                getBalanceResp.status = "200";
            }
            else
            {
                getBalanceResp.status = "500";
                return getBalanceResp;
            }

            return getBalanceResp;
        }
    }
}