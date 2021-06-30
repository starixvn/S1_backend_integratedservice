using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
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
        private readonly int gameType = 0;
        public SexyController(Log2File log2File, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.log2File = log2File;
            thisTp = Tp.Sexy;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            gameType = (int)GameType.Sexy_LiveCasino;
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

        public class Bet : BettingDataLC_Sexy_Bet
        {
            [JsonProperty("action")]
            public string Action { get; set; }

            [JsonProperty("dealerDomain")]
            public string DealerDomain { get; set; }

            [JsonProperty("tableId")]
            public long TableId { get; set; }
        }
        public class Settle : BettingDataLC_Sexy_Settle
        {
            [JsonProperty("action")]
            public string Action { get; set; }

            [JsonProperty("requestId")]
            public string RequestId { get; set; }
        }

        [Route("[controller]")]
        [HttpPost]
        public Resp SeamlessWallet([FromForm] Req req)
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
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.userId, gameType);

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

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, bet.UserId);
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

                try
                {
                    int result = bettingBLL.PlaceBetLC_Sexy(bet, out decimal tpAffterAmount);
                    //Task.Delay(70000).Wait();
                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.UserId, gameType);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.UserId, gameType);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "code:9001";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }
            else if (string.Equals(reqAction.action, "cancelBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    BettingDataLC_Sexy_CancelBet cancelBetTxn = JsonConvert.DeserializeObject<BettingDataLC_Sexy_CancelBet>(req.message);

                    int result = settledBLL.CancelBetLC_Sexy(cancelBetTxn, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.balance = tpAffterAmount.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelBetTxn.userId, gameType);
                        resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "200";
                    }
                    else if (tpAffterAmount < 0 || result == 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.YL, voidFishBetReq.userId, gameType);
                        //resp.balance = tpCustBalance.Balance.ToString("0.00");
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }
            else if (string.Equals(reqAction.action, "settle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Settle settle = JsonConvert.DeserializeObject<Settle>(req.message);

                    int result = settledBLL.SettledLC_Sexy(settle, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.status = "200";
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }
            else if (string.Equals(reqAction.action, "unsettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Unsettle_VoidRound unsettle = JsonConvert.DeserializeObject<Unsettle_VoidRound>(req.message);
                    int result = settledBLL.UnsettledLC_Sexy(unsettle, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.status = "200";
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }
            else if (string.Equals(reqAction.action, "voidRound", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Unsettle_VoidRound voidRound = JsonConvert.DeserializeObject<Unsettle_VoidRound>(req.message);
                    int result = settledBLL.VoidRoundLC_Sexy(voidRound, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.status = "200";
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }
            else if (string.Equals(reqAction.action, "voidTxn", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidTxn voidTxn = JsonConvert.DeserializeObject<VoidTxn>(req.message);
                    int result = settledBLL.VoidTxnLC_Sexy(voidTxn, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        resp.status = "200";
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{reqAction.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{reqAction.action}", ex);
                }
            }

            return resp;
        }
        public class PromotionResp
        {
            public string status { get; set; }
            public string msg { get; set; }
            public decimal balance { get; set; }
            public long balanceTs { get; set; }
        }
        public class PromotionReq
        {
            public string key { get; set; }
            public string action { get; set; }
            public string message { get; set; }
        }

        [Route("[controller]/[action]")]
        [HttpPost]
        public PromotionResp Promotion([FromForm] PromotionReq req)
        {
            PromotionResp resp = new PromotionResp();
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string key = GetSha1($"{thirdPartySetting.TpSetting["cert"]}{new DateTimeOffset(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0)).ToUnixTimeMilliseconds()}");

            if (!req.key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                resp.status = "500";
                resp.msg = "system busy";

                log2File.Error($"Req key={req.key}, key={key}, {new DateTimeOffset(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0)).ToUnixTimeMilliseconds()} \r\nreq={JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{req.action}");
                return resp;
            }

            if (string.Equals(req.action, "treat", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sexy_TreatList = JsonConvert.DeserializeObject<IEnumerable<Sexy_Treat>>(req.message);

                    int result = settledBLL.PrizeLC_Sexy(sexy_TreatList, out decimal afterAmount);

                    if (result > 0 || result == -1)
                    {
                        if(result == -1)
                        {
                            afterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, sexy_TreatList.FirstOrDefault()?.userId, gameType).Balance;
                        }

                        resp.status = "0000";
                        resp.balance = afterAmount;
                        resp.balanceTs = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{req.action}");
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{req.action}", ex);
                }

                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{req.action}");
            }

            return resp;
        }
        private string GetSha1(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            var hashData = new SHA1Managed().ComputeHash(data);
            var hash = string.Empty;
            foreach (var b in hashData)
            {
                hash += b.ToString("X2");
            }
            return hash;
        }
    }
}