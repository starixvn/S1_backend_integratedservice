using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        private readonly IMemoryCache cache;
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private static ConcurrentDictionary<int, ThirdPartySetting> ThirdPartySettingContainer = new ConcurrentDictionary<int, ThirdPartySetting>();
        private static ConcurrentDictionary<string, ThirdPartySetting> ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartySetting>();
        public ThirdPartyBLL(IMemoryCache cache, ThirdPartyService thirdPartyService)
        {
            this.thirdPartyService = thirdPartyService;
            this.cache = cache;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
        }
        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            if (!ThirdPartySettingContainer.ContainsKey(tpId))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartySetting(tpId);
                if (thirdPartyModel != null)
                    ThirdPartySettingContainer.TryAdd(tpId, thirdPartyModel);
            }

            return ThirdPartySettingContainer.ContainsKey(tpId) ? ThirdPartySettingContainer[tpId] : null;
        }

        public ThirdPartySetting GetThirdPartyBySite(int tpId, int siteId)
        {
            string sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartyBySite(tpId, siteId);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
        }
        public TpCheck ThirdPartyCheckWithCache(int tpId, string tpUserName)
        {
            string key = $"TpCheck_{tpId}_{tpUserName}";
            var tpCheck = cache.Get<TpCheck>(key);
            if (tpCheck == null)
            {

                tpCheck = ThirdPartyCheck(tpId, tpUserName);
                cache.Set(key, tpCheck, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(40)));
            }

            return tpCheck;
        }
        public TpCheck ThirdPartyCheck(int tpId, string tpUserName)
        {
            var tpCheck = thirdPartyService.ThirdPartyCheck(tpId, tpUserName);
            return tpCheck;
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            string key = $"{tpId}_{currencyId}";
            if (!TpCurrencyList.ContainsKey(key))
            {
                TpCurrencyList.TryAdd(key, thirdPartyService.GetTpCurrency(tpId, currencyId));
            }

            var tpCurrency = TpCurrencyList[key];
            return tpCurrency;
        }
    }
}
