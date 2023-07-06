using System.Collections.Concurrent;
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

        public TpCustInfoBLL(IServiceProvider provider)
        {
            walletService = provider.GetRequiredService<WalletService>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
            tools = provider.GetRequiredService<Tools>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
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
            var bonusCategory = thirdPartyBLL.GetBonusCategory(gameType);

            // 1.先找到用戶還在Running的CustBonusList
            // 2.再找出符合條件的找到用戶還在Running的CustBonus加到Balance
            // 符合條件:
            //  1.CustBonus.PromoCategoryId = TpGameType.PromoCategoryId
            //  2.CustBonus.PromoCategoryId = 0 (不分類皆可使用，但不包含捕魚類遊戲)
            var custBonus = walletService.GetCustBonusList(tpCustInfo.CustId)
                .FirstOrDefault(i => (int)i.PromoCategoryId == bonusCategory.Id || 
                (i.PromoCategoryId == PromotionCategory.ALL && bonusCategory.CategoryId != (int)TpCategory.Fish));

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
            var tpCustInfo = tpCustInfoService.GetTpCustInfo(tpId, tpUserName, null);
            return tpCustInfo;
        }

        public TpCustInfo GetTpCustInfoByTpCustId(int tpId, string tpCustId)
        {
            var tpCustInfo = tpCustInfoService.GetTpCustInfo(tpId, null, tpCustId);
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
    }
}
