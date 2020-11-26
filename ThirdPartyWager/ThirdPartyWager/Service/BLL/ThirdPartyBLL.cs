using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

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

        public ThirdPartyTimerModel GetThirdPartyTimer(int tpId, int type)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimer(tpId, type);
            return dataTpTimer;
        }

        public int SetThirdPartyTimer(int tpId, int type, long start)
        {
            return thirdPartyHelper.SetThirdPartyTimer(tpId, type, start);
        }

        public ThirdPartyTimerModel GetThirdPartyTimerByReference(int tpId, int type, string reference)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerByReference(tpId, type, reference);
            return dataTpTimer;
        }

        public int SetThirdPartyTimerByReference(int tpId, int type, long start, string reference)
        {
            return thirdPartyHelper.SetThirdPartyTimerByReference(tpId, type, start, reference);
        }
    }
}
