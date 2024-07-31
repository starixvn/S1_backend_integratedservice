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
        private readonly Log2File _log2File;
        private readonly IHostEnvironment _environment;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;
        private readonly Dictionary<string, string> _defaultConfig; // SiteConfig的SiteId=0
        private readonly Dictionary<string, Dictionary<string, string>> _siteNameConfigs;

        public SexyController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _thisTp = Tp.Sexy;
            _gameType = (int)GameType.Sexy_LiveCasino;
            // defaultConfig給原本NOVA、GOAL、BWING的商號使用
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(_thisTp.ToString());
            // 新增商號使用指定siteName
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<Dictionary<string, string>>(_thisTp.ToString());
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

        [Route("[controller]/{sitename}/[action]")]
        [HttpPost]
        public PromotionResp Promotion([FromForm] PromotionReq req, string sitename = "")
        {
            PromotionResp resp = new PromotionResp();

            var config = GetSiteConfig(sitename);
            string key = GetSha1($"{config["cert"]}{new DateTimeOffset(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0)).ToUnixTimeMilliseconds()}");

            if (!req.key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                resp.status = "500";
                resp.msg = "system busy";

                _log2File.Error($"Req key={req.key}, key={key},\r\nreq={JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_{req.action}");
                return resp;
            }

            if (string.Equals(req.action, "treat", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sexy_TreatList = JsonConvert.DeserializeObject<IEnumerable<Sexy_Treat>>(req.message);

                    int result = _settledBLL.PrizeLC_Sexy(sexy_TreatList, out decimal afterAmount);

                    if (result > 0 || result == -1)
                    {
                        if (result == -1)
                        {
                            afterAmount = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, sexy_TreatList.FirstOrDefault()?.userId, _gameType).Balance;
                        }

                        resp.status = "0000";
                        resp.balance = afterAmount;
                        resp.balanceTs = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_{req.action}");
                        return resp;
                    }
                    else
                    {
                        resp.status = "500";
                        resp.msg = "system busy";
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "500";
                    resp.msg = "system busy";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{_thisTp}_{req.action}", ex);
                    return resp;
                }
            }

            _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{_thisTp}_{req.action}");
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

        [Route("[controller]/{sitename}/V2")]
        [HttpPost]
        public dynamic SeamlessWalletV2(string site, [FromForm] Req req, string sitename = "")
        {
            string logName = $"{_thisTp}";
            _log2File.Info(JsonConvert.SerializeObject(req), logName);

            GetSeamlessWalletResp resp = new();
            var config = GetSiteConfig(sitename);
            if (req.key != config["cert2"])
            {
                resp.status = "9999";
                resp.desc = "fail";
                _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            ReqAction reqAction = JsonConvert.DeserializeObject<ReqAction>(req.message);
            logName = $"{_thisTp}_{reqAction.action}";

            if (string.Equals(reqAction.action, "getBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getBalanceReq.userId);
                if (tpCheck == null)
                {
                    resp.status = "1000";
                    resp.desc = "Invalid user Id";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCheck.Locked)
                {
                    resp.status = "1013";
                    resp.desc = "Account is Lock";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getBalanceReq.userId, _gameType);
                if (tpCustBalance != null)
                {
                    GetBalanceResp getBalanceResp = new GetBalanceResp();
                    getBalanceResp.status = "0000";
                    getBalanceResp.userId = getBalanceReq.userId;
                    getBalanceResp.balance = tpCustBalance.Balance;
                    getBalanceResp.balanceTs = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
            }
            else if (string.Equals(reqAction.action, "bet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    BetLC_Sexy bet = JsonConvert.DeserializeObject<BetLC_Sexy>(req.message);

                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, bet.txns.ElementAt(0).userId);
                    if (tpCheck == null)
                    {
                        resp.status = "1012";
                        resp.desc = "Invalid user Id";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (tpCheck.Locked)
                    {
                        resp.status = "1013";
                        resp.desc = "Account is Lock";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (_environment.IsDevelopment() && bet.txns.ElementAt(0).betAmount == 40m)
                    {
                        Thread.Sleep(60000);
                        resp.desc = "fail (test cancel bet)";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    int result = _bettingBLL.PlaceBetLC_AWC_Sexy(bet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, bet.txns.ElementAt(0).userId, _gameType);
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.status = "1018";
                        resp.desc = "Not Enough Balance";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "cancelBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CancelLC_Sexy cancelBet = JsonConvert.DeserializeObject<CancelLC_Sexy>(req.message);

                    int result = _settledBLL.CancelBetLC_AWC_Sexy(cancelBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, cancelBet.txns.ElementAt(0).userId, _gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "voidBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidBetLC_Sexy voidBet = JsonConvert.DeserializeObject<VoidBetLC_Sexy>(req.message);

                    int result = _settledBLL.VoidBetLC_AWC_Sexy(voidBet);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "settle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    PayoutLC_Sexy payout = JsonConvert.DeserializeObject<PayoutLC_Sexy>(req.message);

                    int result = _settledBLL.PayoutLC_AWC_Sexy(payout, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "unsettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnPayoutLC_Sexy unpayout = JsonConvert.DeserializeObject<UnPayoutLC_Sexy>(req.message);

                    int result = _settledBLL.UnPayoutLC_AWC_Sexy(unpayout);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "voidSettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidSettleLC_Sexy voidSettle = JsonConvert.DeserializeObject<VoidSettleLC_Sexy>(req.message);

                    int result = _settledBLL.VoidSettleLC_AWC_Sexy(voidSettle);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "give", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RewardLC_Sexy reward = JsonConvert.DeserializeObject<RewardLC_Sexy>(req.message);

                    int result = _settledBLL.RewardLC_AWC_Sexy(reward, out decimal tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        GetSeamlessWalletResp getGiveResp = new GetSeamlessWalletResp();
                        getGiveResp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getGiveResp)}", logName);
                        return getGiveResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "tip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    TipLC_Sexy tip = JsonConvert.DeserializeObject<TipLC_Sexy>(req.message);

                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tip.txns.ElementAt(0).userId);
                    if (tpCheck == null)
                    {
                        resp.status = "1012";
                        resp.desc = "Invalid user Id";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (tpCheck.Locked)
                    {
                        resp.status = "1013";
                        resp.desc = "Account is Lock";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    if (_environment.IsDevelopment() && tip.txns.ElementAt(0).tip == 40m)
                    {
                        Thread.Sleep(60000);
                        resp.desc = "fail (test cancel tip)";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    int result = _bettingBLL.TipLC_AWC_Sexy(tip, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tip.txns.ElementAt(0).userId, _gameType);
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.status = "1018";
                        resp.desc = "Not Enough Balance";
                        _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "cancelTip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CancelTipLC_Sexy cancelTip = JsonConvert.DeserializeObject<CancelTipLC_Sexy>(req.message);

                    int result = _settledBLL.CancelTipLC_AWC_Sexy(cancelTip, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        getBetResp.status = "0000";
                        getBetResp.balance = tpAffterAmount;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                    else if (result == -1)
                    {
                        GetBetResp getBetResp = new GetBetResp();
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, cancelTip.txns.ElementAt(0).userId, _gameType);
                        getBetResp.status = "0000";
                        getBetResp.balance = tpCustBalance.Balance;
                        getBetResp.balanceTs = GetCurrentGameTime();
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                        return getBetResp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            else if (string.Equals(reqAction.action, "resettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ResettleLC_Sexy resettle = JsonConvert.DeserializeObject<ResettleLC_Sexy>(req.message);

                    int result = _settledBLL.ResettleLC_AWC_Sexy(resettle);
                    if (result > 0 || result == -1)
                    {
                        resp.status = "0000";
                        _log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex.Message}", logName, ex);
                    return resp;
                }
            }
            resp.status = "9999";
            resp.desc = "fail";
            _log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        private static string GetCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

        private Dictionary<string, string> GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return _defaultConfig;
            }

            return _siteNameConfigs.TryGetValue(siteName, out var result) ? result : _defaultConfig;
        }
    }
}