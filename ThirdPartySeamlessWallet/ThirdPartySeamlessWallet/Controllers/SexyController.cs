using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class SexyController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        public SexyController(Log2File log2File, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.log2File = log2File;
            thisTp = Tp.Sexy;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
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

        public class Bet
        {
            [JsonProperty("action")]
            public string Action { get; set; }

            [JsonProperty("userId")]
            public string UserId { get; set; }

            [JsonProperty("IP")]
            public string Ip { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("dealerDomain")]
            public string DealerDomain { get; set; }

            [JsonProperty("tableId")]
            public long TableId { get; set; }

            [JsonProperty("roundId")]
            public string RoundId { get; set; }

            [JsonProperty("roundStartTime")]
            public string RoundStartTime { get; set; }

            [JsonProperty("txns")]
            public List<BetTxn> Txns { get; set; }

            [JsonProperty("requestId")]
            public string RequestId { get; set; }

            [JsonProperty("validBetTime")]
            public long ValidBetTime { get; set; }
        }

        public class BetTxn
        {
            [JsonProperty("txId")]
            public string TxId { get; set; }

            [JsonProperty("gameType")]
            public string GameType { get; set; }

            [JsonProperty("betAmount")]
            public decimal BetAmount { get; set; }

            [JsonProperty("betTime")]
            public string BetTime { get; set; }

            [JsonProperty("updateTime")]
            public string UpdateTime { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }
        }

        public class Settle
        {
            [JsonProperty("action")]
            public string Action { get; set; }

            [JsonProperty("requestId")]
            public string RequestId { get; set; }

            [JsonProperty("validBetTime")]
            public long ValidBetTime { get; set; }

            [JsonProperty("dealerDomain")]
            public string DealerDomain { get; set; }

            [JsonProperty("tableId")]
            public long TableId { get; set; }

            [JsonProperty("roundId")]
            public string RoundId { get; set; }

            [JsonProperty("roundStartTime")]
            public string RoundStartTime { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("winner")]
            public string Winner { get; set; }

            [JsonProperty("txns")]
            public List<SettleTxn> Txns { get; set; }
        }

        public class SettleTxn
        {
            [JsonProperty("txId")]
            public string TxId { get; set; }

            [JsonProperty("gameType")]
            public string GameType { get; set; }

            [JsonProperty("betAmount")]
            public decimal BetAmount { get; set; }

            [JsonProperty("updateTime")]
            public string UpdateTime { get; set; }

            [JsonProperty("betTime")]
            public string BetTime { get; set; }

            [JsonProperty("userId")]
            public string UserId { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("winLoss")]
            public decimal WinLoss { get; set; }

            [JsonProperty("odds")]
            public decimal Odds { get; set; }

            [JsonProperty("settleWinLoss")]
            public decimal SettleWinLoss { get; set; }

            [JsonProperty("validBet")]
            public decimal ValidBet { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }

        [Route("[controller]")]
        [HttpPost]
        public Resp SeamlessWallet([FromForm]Req req)
        {
            Resp resp = new Resp();
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

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
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.userId);
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
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.userId);

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

                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
            }
            else if (string.Equals(reqAction.action, "bet", StringComparison.OrdinalIgnoreCase))
            {
                Bet bet = JsonConvert.DeserializeObject<Bet>(req.message);

                try
                {
                    int result = bettingBLL.PlaceBetRNG_Sexy(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, settleFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "code:9001";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }
            else if (string.Equals(reqAction.action, "cancelBet", StringComparison.OrdinalIgnoreCase))
            {
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");

                try
                {
                    int result = settledBLL.RefundFish_YL(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                    log2File.Info(voidFishBetLog, "YL_VoidFishBet");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }
            else if (string.Equals(reqAction.action, "settle", StringComparison.OrdinalIgnoreCase))
            {
                Settle settle = JsonConvert.DeserializeObject<Settle>(req.message);

                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");

                try
                {
                    int result = settledBLL.RefundFish_YL(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                    log2File.Info(voidFishBetLog, "YL_VoidFishBet");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }
            else if (string.Equals(reqAction.action, "unsettle", StringComparison.OrdinalIgnoreCase))
            {
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");

                try
                {
                    int result = settledBLL.RefundFish_YL(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                    log2File.Info(voidFishBetLog, "YL_VoidFishBet");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }
            else if (string.Equals(reqAction.action, "voidRound", StringComparison.OrdinalIgnoreCase))
            {
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");

                try
                {
                    int result = settledBLL.RefundFish_YL(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                    log2File.Info(voidFishBetLog, "YL_VoidFishBet");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }
            else if (string.Equals(reqAction.action, "voidTxn", StringComparison.OrdinalIgnoreCase))
            {
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");

                try
                {
                    int result = settledBLL.RefundFish_YL(null, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    string voidFishBetLog = $"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}";
                    log2File.Info(voidFishBetLog, "YL_VoidFishBet");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}");
                }
            }

            return resp;
        }
    }
}