using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly IMemoryCache cache;

        public ThirdPartyBLL(ThirdPartyHelper thirdPartyHelper,TpCustInfoService tpCustInfoService, IMemoryCache cache)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.tpCustInfoService = tpCustInfoService;
            this.cache = cache;
        }
        public Dictionary<string, string> GetThirdPartySetting(int tpId)
        {
            var key = $"Setting_{tpId}";
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
            var key = $"Setting_{tpId}_{siteId}";
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
            var key = $"Setting_{tpId}";
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

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByCurrency(int tpId, int type, string currency, int gameType = 0)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerReferenceByCurrency(tpId, type, currency, gameType);
            return dataTpTimer;
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByGameType(int tpId, int type, int gameType)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerReferenceByGameType(tpId, type, gameType);
            return dataTpTimer;
        }

        public int SetThirdPartyTimerByCurrency(int tpId, int type, long start, string currency, int gameType = 0)
        {
            return thirdPartyHelper.SetThirdPartyTimerByCurrency(tpId, type, start, currency, gameType);
        }

        public int SetThirdPartyTimerByGameType(int tpId, int type, long start, int gameType)
        {
            return thirdPartyHelper.SetThirdPartyTimerByGameType(tpId, type, start, gameType);
        }
        
        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            var key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return tpCustInfo;
        }

        private TpCustInfo GetTpCustInfo(int tpId, string tpUserName)
        {
            var tpCustInfo = tpCustInfoService.GetTpCustInfo(tpId, tpUserName);
            return tpCustInfo;
        }
        
        public SettlementType GetGameCasinoSettlementType(int tpId, string gameId)
        {
            var settlementType = SettlementType.SettledForValid;
            var casino = GetGameCasinoWithCache(tpId);
            var check = casino.FirstOrDefault(x => x.GameId == gameId && x.TypeId == 3);
            if (check != null)
            {
                settlementType = SettlementType.SettledForValidNotRollover;
            }
            return settlementType;
        }

        private IEnumerable<TpGameCasino> GetGameCasinoWithCache(int tpId)
        {
            string key = $"TpGameCasino_{tpId}";
            var tpGameCasino = cache.Get<IEnumerable<TpGameCasino>>(key);
            if (tpGameCasino == null)
            {
                tpGameCasino = thirdPartyHelper.GetGameCasino(tpId);
                cache.Set(key, tpGameCasino, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
            }

            return tpGameCasino;
        }
    }
}
