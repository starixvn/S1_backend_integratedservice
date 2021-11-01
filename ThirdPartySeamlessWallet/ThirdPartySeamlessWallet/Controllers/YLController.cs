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
        private readonly int gameType = 0;

        public YLController(ILogger<YLController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.logger = logger;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.log2File = log2File;
            this.settledBLL = settledBLL;
            this.gameType = (int)GameType.YL_Fish;
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
        public Resp SeamlessWallet([FromForm] Req req)
        {
            Resp resp = new Resp();
            string logName = Tp.YL.ToString();
            var content = JsonConvert.SerializeObject(req);
            log2File.Info(content, logName);

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            ReqAction reqAction = JsonConvert.DeserializeObject<ReqAction>(req.message);
            logName = $"{logName}_{reqAction.action}";

            if (string.Equals(reqAction.action, "GetBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.YL, getBalanceReq.userId);
                if (tpCheck == null || tpCheck.Locked)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, getBalanceReq.userId);

                if (tpCustBalance != null)
                {
                    resp.balance = tpCustBalance.Balance.ToString("0.##");
                    resp.status = "200";
                    log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                else
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "settleFishBet", StringComparison.OrdinalIgnoreCase))
            {
                SettleFishBetReq settleFishBetReq = JsonConvert.DeserializeObject<SettleFishBetReq>(req.message);
                try
                {
                    int result = settledBLL.SettledFish_YL(settleFishBetReq, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        if (tpAffterAmount == 0)
                        {
                            tpAffterAmount = tpCustInfoBLL.GetBalance((int)Tp.YL, settleFishBetReq.userId).Balance;
                        }
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                        log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                        log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else if (result == -2)
                    {
                        // 計算餘額為負數 阻擋此次下注 遊戲會顯示餘額不足
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "code:9001";
                        log2File.Error($"check balance negative\r\n{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "code:9001";
                    }

                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "voidFishBet", StringComparison.OrdinalIgnoreCase))
            {
                VoidFishBetReq voidFishBetReq = JsonConvert.DeserializeObject<VoidFishBetReq>(req.message);
                try
                {
                    int result = settledBLL.RefundFish_YL(voidFishBetReq, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                        log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                        log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else
            {
                log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }

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
        public Resp Bet([FromForm] Req req)
        {
            string logName = $"{Tp.YL}_BetV2";
            var content = JsonConvert.SerializeObject(req);
            BetReq betReq = JsonConvert.DeserializeObject<BetReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            Resp resp = new Resp();

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.YL, betReq.userId);
            if (tpCheck == null || tpCheck.Locked)
            {
                resp.status = "500";
                resp.msg = "code:9003";
                log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
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
        public Resp VoidFishBet([FromForm] Req req)
        {
            string logName = $"{Tp.YL}_VoidFishBetV2";
            var content = JsonConvert.SerializeObject(req);
            log2File.Info(content, logName);

            VoidFishBetReq voidFishBetReq = JsonConvert.DeserializeObject<VoidFishBetReq>(req.message);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.YL);
            Resp resp = new Resp();

            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "500";
                resp.msg = "system busy";
                log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }
    }
}