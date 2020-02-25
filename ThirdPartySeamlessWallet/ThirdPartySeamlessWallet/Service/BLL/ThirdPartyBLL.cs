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
        public ThirdPartyBLL(IMemoryCache cache, ThirdPartyService thirdPartyService)
        {
            this.thirdPartyService = thirdPartyService;
            this.cache = cache;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
        }
        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            return thirdPartyService.GetThirdPartySetting(tpId);
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
