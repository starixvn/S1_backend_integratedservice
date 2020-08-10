using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly IMemoryCache cache;

        public ThirdPartyBLL(ThirdPartyHelper thirdPartyHelper, IMemoryCache cache)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.cache = cache;
        }
        public Dictionary<string, string> GetThirdPartySetting(int tpId)
        {
            string key = $"Setting_{tpId}";
            var setting = cache.Get<Dictionary<string, string>>(key);
            if (setting == null)
            {
                var dataTpList = thirdPartyHelper.GetThirdPartySetting(tpId);
                if (dataTpList != null)
                {
                    setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataTpList.Setting);
                    cache.Set(key, setting, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return setting;
        }
    }
}
