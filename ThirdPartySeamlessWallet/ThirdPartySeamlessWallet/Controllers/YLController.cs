using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class YLController : ControllerBase
    {
        private readonly ILogger<YLController> logger;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Log2File log2File;
        private readonly SettledBLL settledBLL;

        public YLController(ILogger<YLController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.logger = logger;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.log2File = log2File;
            this.settledBLL = settledBLL;
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

        public class ReqAction
        {
            public string action { get; set; }
        }

        public class GetBalanceReq
        {
            public string userId { get; set; }
        }

        [Route("[controller]")]
        [HttpPost]
        public Resp SeamlessWallet([FromForm]Req req)
        {
            Resp resp = new Resp();
            logger.LogCritical(JsonConvert.SerializeObject(req));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }

            ReqAction reqAction = JsonConvert.DeserializeObject<ReqAction>(req.message);
            if (string.Equals(reqAction.action, "GetBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);
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

                string getBalanceLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                log2File.Info(getBalanceLog, "YL_GetBalanceLog");
            }
            else if (string.Equals(reqAction.action, "settleFishBet", StringComparison.OrdinalIgnoreCase))
            {
                SettleFishBetReq settleFishBetReq = JsonConvert.DeserializeObject<SettleFishBetReq>(req.message);
                int result = settledBLL.SettledFish_YL(settleFishBetReq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    resp.balance = tpAffterAmount.ToString("0.00");
                    resp.status = "200";
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                    resp.balance = tpCustBalance.Balance.ToString("0.00");
                    resp.status = "200";
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                    resp.balance = tpCustBalance.Balance.ToString("0.00");
                    resp.status = "500";
                    resp.msg = "code:9001";
                }

                string settleFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                log2File.Info(settleFishBetLog, "YL_SettleFishBet");
            }
            else if (string.Equals(reqAction.action, "voidFishBet", StringComparison.OrdinalIgnoreCase))
            {
                VoidFishBetReq voidFishBetReq = JsonConvert.DeserializeObject<VoidFishBetReq>(req.message);
                int result = settledBLL.RefundFish_YL(voidFishBetReq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    resp.balance = tpAffterAmount.ToString("0.00");
                    resp.status = "200";
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                    resp.balance = tpCustBalance.Balance.ToString("0.00");
                    resp.status = "200";
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                    resp.balance = tpCustBalance.Balance.ToString("0.00");
                    resp.status = "500";
                    resp.msg = "code:9001";
                }

                string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                log2File.Info(voidFishBetLog, "YL_VoidFishBet");
            }

            logger.LogCritical(JsonConvert.SerializeObject(resp));
            return resp;
        }

        public class BetReq
        {
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

        public class SettleFishBetReq : BettingDataFish_YL
        {
            public string extension1 { get; set; }
        }

        public class VoidFishBetReq : RefundDataFish_YL
        {

        }
        [Route("[controller]/[action]")]
        [HttpPost]
        public Resp VoidFishBet([FromForm]Req req)
        {
            //logger.LogCritical(JsonConvert.SerializeObject(req));
            VoidFishBetReq voidFishBetReq = JsonConvert.DeserializeObject<VoidFishBetReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            Resp resp = new Resp();
            log2File.Info(JsonConvert.SerializeObject(req), "YL_VoidFishBet");

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                return resp;
            }

            string voidFishBetReqLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
            log2File.Info(voidFishBetReqLog, "YL_SettleFishBet");
            //logger.LogCritical(JsonConvert.SerializeObject(resp));
            return resp;
        }
    }
}