using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpCustInfoBLL
    {
        private readonly WalletService walletService;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly Tools tools;
        private ConcurrentDictionary<string, TpCountryCode> tpCountryCodeCache = new();
        private readonly ICacheHelper _redisHelper;
        private readonly BonusWalletBLL _bonusWalletBLL;
        private readonly ILogger<TpCustInfoBLL> _logger;

        public TpCustInfoBLL(IServiceProvider provider)
        {
            walletService = provider.GetRequiredService<WalletService>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
            tools = provider.GetRequiredService<Tools>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _bonusWalletBLL = provider.GetRequiredService<BonusWalletBLL>();
            _logger = provider.GetRequiredService<ILogger<TpCustInfoBLL>>();
        }

        public TpCustBalance GetBalanceByHalfSecCache(int tpId, string tpUserName, int gameType = 0)
        {
            var tpCustBalance = _redisHelper.TryGetOrCreate(
                $"TpCustBalance:{tpId}:{tpUserName}",
                TimeSpan.FromSeconds(0.5),
                () => GetBalance(tpId, tpUserName, gameType));
            return tpCustBalance;
        }

        public TpCustBalance GetBalance(int tpId, string tpUserName, int gameType = 0)
        {
            var tpCustBalance = new TpCustBalance();
            var tpCustInfo = GetTpCustInfoWithCache(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var balance = walletService.GetBalance(tpCustInfo.CustId);
            
            var custBonus = _bonusWalletBLL.GetCustBonus(tpCustInfo.CustId, gameType);
            if (custBonus != null)
            {
                balance += custBonus.Bonus;
            }

            tpCustBalance.Balance = balance / tpCurrency.ExchangeRate;
            tpCustBalance.Balance = tools.Floor(tpCustBalance.Balance, 4);
            tpCustBalance.ExchangeRate = tpCurrency.ExchangeRate;
            tpCustBalance.TpCurrencyId = tpCurrency.TpCurrencyId;
            return tpCustBalance;
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            
            var tpCustInfo = _redisHelper.TryGetOrCreate(
                $"TpCustInfo:{tpId}:{tpUserName}",
                TimeSpan.FromMinutes(1),
               () => GetTpCustInfo(tpId, tpUserName));
            _logger.LogError($"tpId={tpId},tpUserName={tpUserName},GetTpCustInfoWithCache={JsonConvert.SerializeObject(tpCustInfo)}");
            return tpCustInfo;
        }

        public TpCustInfo GetTpCustInfoWithCacheByTpCustId(int tpId, string tpCustId)
        {
            var tpCustInfo = _redisHelper.TryGetOrCreate(
               $"TpCustInfo:{tpId}:{tpCustId}",
               TimeSpan.FromMinutes(1),
              () => GetTpCustInfoByTpCustId(tpId, tpCustId));
            return tpCustInfo;
        }

        public TpCustInfo GetTpCustInfo(int tpId, string tpUserName)
        {
            var tpCustInfo = tpCustInfoService.GetTpCustInfoByTpUserName(tpId, tpUserName);
            _logger.LogError($"tpId={tpId},tpUserName={tpUserName},GetTpCustInfo={JsonConvert.SerializeObject(tpCustInfo)}");
            return tpCustInfo;
        }

        public TpCustInfo GetTpCustInfoByTpCustId(int tpId, string tpCustId)
        {
            var tpCustInfo = tpCustInfoService.GetTpCustInfoByTpCustId(tpId, tpCustId);
            return tpCustInfo;
        }

        public int SetCustGotoGame(int custId, int tpId, int gameType)
        {
            return tpCustInfoService.SetCustGotoGame(custId, tpId, gameType);
        }

        public int GetCustGotoGameType(int custId, int tpId)
        {
            return tpCustInfoService.GetCustGotoGameType(custId, tpId);
        }

        public int SetTpPwd(int tpId, string tpUserName, string tpPwd)
        {
            return tpCustInfoService.SetTpPwd(tpId, tpUserName, tpPwd);
        }

        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            tpCustInfoService.AddLoginRecord(custId, siteId, ip, out token);
        }

        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            tpCustInfoService.AddUserLoginLog(custId, userName, siteId, loginIp, fromUrl, userAgent);
        }

        public TpCountryCode GetTpCurrencyCodeByCurrencyId(int tpId, string tpCurrencyId)
        {
            var key = $"GetTpCurrencyCodeByCurrencyId{tpId}{tpCurrencyId}";
            if (tpCountryCodeCache.ContainsKey(key))
            {
                return tpCountryCodeCache[key];
            }
            else
            {
                var tpCountryCode = tpCustInfoService.GetTpCurrencyCodeByCurrencyId(tpId, tpCurrencyId);
                tpCountryCodeCache.TryAdd(key, tpCountryCode);
                return tpCountryCode;
            }
        }

        public TpCountryCode GetTpCurrencyCode(int tpId, int currencyId)
        {
            var key = $"GetTpCurrencyCode{tpId}{currencyId}";
            if (tpCountryCodeCache.ContainsKey(key))
            {
                return tpCountryCodeCache[key];
            }
            else
            {
                var tpCountryCode = tpCustInfoService.GetTpCurrencyCode(tpId, currencyId);
                tpCountryCodeCache.TryAdd(key, tpCountryCode);
                return tpCountryCode;
            }
        }

        public int UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            return tpCustInfoService.UpdateOnline(custId, isOnline, token, ip);
        }

        public CustProfile GetCustProfileWithCacheByUserName(int siteId, string userName, string sabaId)
        {
            var custProfile = _redisHelper.TryGetOrCreate(
               $"CustProfile:{siteId}:{userName}:{sabaId}",
               TimeSpan.FromMinutes(10),
              () => GetCustProfileByUserName(siteId, userName, sabaId));
            return custProfile;
        }
        public CustProfile GetCustProfileByUserName(int siteId, string userName, string sabaId)
        {
            var custId = TpIdToCustId(string.IsNullOrEmpty(sabaId) ? string.Empty : sabaId);
            return tpCustInfoService.GetCustProfileByUserName(custId, siteId, userName);
        }
        private int? TpIdToCustId(string TpId)
        {
            TpId = TpId.Trim();

            if (TpId.IndexOf("_") > -1)
            {
                TpId = TpId.Substring(TpId.LastIndexOf("_"), TpId.Length - TpId.LastIndexOf("_"));
            }
            else if (TpId.IndexOf("x") > -1)
            {
                TpId = TpId.Substring(TpId.LastIndexOf("x"), TpId.Length - TpId.LastIndexOf("x"));
            }

            var m = Regex.Match(TpId, @"\d+");
            var _custId = int.TryParse(m.Value, out var custId) ? custId : (int?)null;

            return _custId;
        }
    }
}
