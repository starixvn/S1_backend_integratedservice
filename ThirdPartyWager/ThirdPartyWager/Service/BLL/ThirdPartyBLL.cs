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
        public Dictionary<string, string> GetThirdPartyBySite(int tpId, int siteId)
        {
            string key = $"Setting_{tpId}_{siteId}";
            var setting = cache.Get<Dictionary<string, string>>(key);
            if (setting == null)
            {
                var dataTpList = thirdPartyHelper.GetThirdPartyBySite(tpId, siteId);
                if (dataTpList != null)
                {
                    setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataTpList.Setting);
                    cache.Set(key, setting, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return setting;
        }

        public int SetThirdPartySetting(int tpId, Dictionary<string, string> setting)
        {
            string key = $"Setting_{tpId}";
            var result = thirdPartyHelper.SetThirdPartySetting(tpId, JsonConvert.SerializeObject(setting));
            if (result > 0)
            {
                cache.Set(key, setting, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }

            return result;
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

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByCurrency(int tpId, int type, string currency)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerReferenceByCurrency(tpId, type, currency);
            return dataTpTimer;
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByGameType(int tpId, int type, int gameType)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerReferenceByGameType(tpId, type, gameType);
            return dataTpTimer;
        }

        public int SetThirdPartyTimerByCurrency(int tpId, int type, long start, string currency)
        {
            return thirdPartyHelper.SetThirdPartyTimerByCurrency(tpId, type, start, currency);
        }

        public int SetThirdPartyTimerByGameType(int tpId, int type, long start, int gameType)
        {
            return thirdPartyHelper.SetThirdPartyTimerByGameType(tpId, type, start, gameType);
        }
    }
}
