using System.Security.Cryptography;
using System.Text;

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
        private readonly IHostEnvironment environment;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        private readonly int gameType = 0;

        public SexyController(IServiceProvider provider)
        {
            log2File = provider.GetRequiredService<Log2File>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            thisTp = Tp.Sexy;
            gameType = (int)GameType.Sexy_LiveCasino;
        }

        #region Request

        public class Req
        {
            public string key { get; set; }
            public string message { get; set; }
        }

        public class ReqAction
        {
            public string action { get; set; }
        }

        public class GetBalanceReq
        {
            public string userId { get; set; }
        }
        public class PromotionReq
        {
            public string key { get; set; }
            public string action { get; set; }
            public string message { get; set; }
        }

        #endregion

        #region Response

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

        public class PromotionResp
        {
            public string status { get; set; }
            public string msg { get; set; }
            public decimal balance { get; set; }
            public long balanceTs { get; set; }
        }

        #endregion

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

        private static string GetSha1(string value)
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

        [Route("[controller]/V2")]
        [HttpPost]
        public dynamic SeamlessWalletV2(string site, [FromForm] Req req)
        {
            string logName = $"{thisTp}";
            log2File.Info(JsonConvert.SerializeObject(req), logName);

            GetSeamlessWalletResp resp = new();

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
                    getBalanceResp.balanceTs = GetCurrentGameTime();
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
                        Thread.Sleep(60000);
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
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.txns.ElementAt(0).userId, gameType);
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
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
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelBet.txns.ElementAt(0).userId, gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "give", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RewardLC_Sexy reward = JsonConvert.DeserializeObject<RewardLC_Sexy>(req.message);

                    int result = settledBLL.RewardLC_AWC_Sexy(reward, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        GetSeamlessWalletResp getGiveResp = new GetSeamlessWalletResp();
                        getGiveResp.status = "0000";
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getGiveResp)}", logName);
                        return getGiveResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "tip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    TipLC_Sexy tip = JsonConvert.DeserializeObject<TipLC_Sexy>(req.message);

                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tip.txns.ElementAt(0).userId);
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

                    if (environment.IsDevelopment() && tip.txns.ElementAt(0).tip == 40m)
                    {
                        Thread.Sleep(60000);
                        resp.desc = "fail (test cancel tip)";
                        log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    int result = bettingBLL.TipLC_AWC_Sexy(tip, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tip.txns.ElementAt(0).userId, gameType);
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "cancelTip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CancelTipLC_Sexy cancelTip = JsonConvert.DeserializeObject<CancelTipLC_Sexy>(req.message);

                    int result = settledBLL.CancelTipLC_AWC_Sexy(cancelTip, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelTip.txns.ElementAt(0).userId, gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "resettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ResettleLC_Sexy resettle = JsonConvert.DeserializeObject<ResettleLC_Sexy>(req.message);

                    int result = settledBLL.ResettleLC_AWC_Sexy(resettle);
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
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            resp.status = "9999";
            resp.desc = "fail";
            log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        private static string GetCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }
    }
}