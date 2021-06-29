using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
    [ApiController]
    public class SexyController : ControllerBase
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<SexyController> logger;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly int gameType = 0;
        public SexyController(
            IHostEnvironment environment,
            ILogger<SexyController> logger,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL
        )
        {
            this.environment = environment;
            this.logger = logger;
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            thisTp = Tp.Sexy;
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
                        if (result == -1)
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

        #region Request

        #endregion

        #region Response

        //[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        private class GetSeamlessWalletResp
        {
            public string status { get; set; }
            public string desc { get; set; }
        }

        private class GetBalanceResp : GetSeamlessWalletResp
        {
            public decimal balance { get; set; }
            public string balanceTs { get; set; }
            public string userId { get; set; }
        }

        private class GetBetResp : GetSeamlessWalletResp
        {
            public decimal balance { get; set; }
            public string balanceTs { get; set; }
        }

        private class GetGiveResp : GetSeamlessWalletResp
        {
            public decimal balance { get; set; }
            public string balanceTs { get; set; }
        }

        #endregion

        [Route("[controller]/V2")]
        [HttpPost]
        public dynamic SeamlessWalletV2([FromForm] Req req)
        {
            string logName = $"{thisTp}";
            log2File.Info(JsonConvert.SerializeObject(req), logName);

            GetSeamlessWalletResp resp = new GetSeamlessWalletResp();

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (req.key != thirdPartySetting.TpSetting["cert2"])
            {
                resp.status = "9999";
                resp.desc = "fail";
                log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            ReqAction reqAction = JsonConvert.DeserializeObject<ReqAction>(req.message);
            logName = $"{thisTp}_{reqAction.action}";

            if (string.Equals(reqAction.action, "getBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.userId);
                if (tpCheck == null)
                {
                    resp.status = "1000";
                    resp.desc = "Invalid user Id";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCheck.Locked)
                {
                    resp.status = "1013";
                    resp.desc = "Account is Lock";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.userId, gameType);
                if (tpCustBalance != null)
                {
                    GetBalanceResp getBalanceResp = new GetBalanceResp();
                    getBalanceResp.status = "0000";
                    getBalanceResp.userId = getBalanceReq.userId;
                    getBalanceResp.balance = tpCustBalance.Balance;
                    getBalanceResp.balanceTs = getCurrentGameTime();
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
            }
            else if (string.Equals(reqAction.action, "bet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    BetLC_Sexy bet = JsonConvert.DeserializeObject<BetLC_Sexy>(req.message);

                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, bet.txns.ElementAt(0).userId);
                    if (tpCheck == null)
                    {
                        resp.status = "1012";
                        resp.desc = "Invalid user Id";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (tpCheck.Locked)
                    {
                        resp.status = "1013";
                        resp.desc = "Account is Lock";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (environment.IsDevelopment() && bet.txns.ElementAt(0).betAmount == 40m)
                    {
                        System.Threading.Thread.Sleep(60000);
                        resp.desc = "fail (test cancel bet)";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    int result = bettingBLL.PlaceBetLC_AWC_Sexy(bet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.txns.ElementAt(0).userId, gameType);
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.status = "1018";
                        resp.desc = "Not Enough Balance";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "cancelBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CancelLC_Sexy cancelBet = JsonConvert.DeserializeObject<CancelLC_Sexy>(req.message);

                    int result = settledBLL.CancelBetLC_AWC_Sexy(cancelBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelBet.txns.ElementAt(0).userId, gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "adjustBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    AdjustBetLC_Sexy adjustBet = JsonConvert.DeserializeObject<AdjustBetLC_Sexy>(req.message);

                    int result = settledBLL.AdjustBetLC_AWC_Sexy(adjustBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, adjustBet.txns.ElementAt(0).userId, gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "voidBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidBetLC_Sexy voidBet = JsonConvert.DeserializeObject<VoidBetLC_Sexy>(req.message);

                    int result = settledBLL.VoidBetLC_AWC_Sexy(voidBet);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "unvoidBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnVoidBetLC_Sexy unvoidBet = JsonConvert.DeserializeObject<UnVoidBetLC_Sexy>(req.message);

                    int result = settledBLL.UnvoidBetLC_Sexy(unvoidBet);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "settle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    PayoutLC_Sexy payout = JsonConvert.DeserializeObject<PayoutLC_Sexy>(req.message);

                    int result = settledBLL.PayoutLC_AWC_Sexy(payout, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "unsettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnPayoutLC_Sexy unpayout = JsonConvert.DeserializeObject<UnPayoutLC_Sexy>(req.message);

                    int result = settledBLL.UnPayoutLC_AWC_Sexy(unpayout);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "voidSettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidSettleLC_Sexy voidSettle = JsonConvert.DeserializeObject<VoidSettleLC_Sexy>(req.message);

                    int result = settledBLL.VoidSettleLC_AWC_Sexy(voidSettle);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "unvoidSettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnVoidSettleLC_Sexy unvoidSettle = JsonConvert.DeserializeObject<UnVoidSettleLC_Sexy>(req.message);

                    int result = settledBLL.UnvoidSettleLC_AWC_Sexy(unvoidSettle);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "refund", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RefundLC_Sexy refund = JsonConvert.DeserializeObject<RefundLC_Sexy>(req.message);
                    int cnt = refund.txns.Count();
                    if (cnt % 2 != 0)
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else
                    {
                        Dictionary<string, RefundLC_Sexy> keyValuePairs = new Dictionary<string, RefundLC_Sexy>();
                        foreach (var item in refund.txns)
                        {
                            RefundLC_Sexy refRefundList;
                            if (keyValuePairs.ContainsKey(item.refundPlatformTxId))
                            {
                                refRefundList = keyValuePairs[item.refundPlatformTxId];
                            }
                            else
                            {
                                refRefundList = new RefundLC_Sexy();
                                keyValuePairs.Add(item.refundPlatformTxId, refRefundList);
                                refRefundList.txns = new List<RefundDataLC_Sexy>();
                            }
                            refRefundList.txns.Add(item);
                        }

                        var segment = keyValuePairs.Values.ToList();
                        foreach (var record in segment)
                        {
                            int result = settledBLL.RefundLC_AWC_Sexy(record);
                            if (result > 0 || result == -1)
                            {
                                resp.status = "0000";
                                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            }
                            else
                            {
                                resp.status = "9999";
                                resp.desc = "fail";
                                log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                                return resp;
                            }
                        }
                    }
                    return resp;
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "give", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RewardLC_Sexy reward = JsonConvert.DeserializeObject<RewardLC_Sexy>(req.message);

                    int result = settledBLL.RewardLC_AWC_Sexy(reward, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetGiveResp getGiveResp = new GetGiveResp();
                        getGiveResp.status = "0000";
                        getGiveResp.balance = tpAffterAmount;
                        getGiveResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getGiveResp)}", logName);
                        return getGiveResp;
                    }
                    else if (result == -1)
                    {
                        GetGiveResp getGiveResp = new GetGiveResp();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, reward.txns.ElementAt(0).userId, gameType);
                        getGiveResp.status = "0000";
                        getGiveResp.balance = tpCustBalance.Balance;
                        getGiveResp.balanceTs = getCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getGiveResp)}", logName);
                        return getGiveResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    return resp;
                }
            }

            resp.status = "9999";
            resp.desc = "fail";
            log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        private string getCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }
    }
}