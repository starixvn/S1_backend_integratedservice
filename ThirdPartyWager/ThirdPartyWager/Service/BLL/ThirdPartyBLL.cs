using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ThirdPartyBLL : IThirdPartyBLL
    {
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly IMemoryCache cache;

        public ThirdPartyBLL(ThirdPartyHelper thirdPartyHelper, TpCustInfoService tpCustInfoService, IMemoryCache cache)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.tpCustInfoService = tpCustInfoService;
            this.cache = cache;
        }

        [Obsolete("Please use ConfigBLL.GetConfigValuesBySiteId")]
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

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByReference(int tpId, int type, int gameType, string reference)
        {
            var dataTpTimer = thirdPartyHelper.GetThirdPartyTimerReferenceByReference(tpId, type, gameType, reference);
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

        public int SetThirdPartyTimerByReference(int tpId, int type, long start, int gameType, string reference)
        {
            return thirdPartyHelper.SetThirdPartyTimerByReference(tpId, type, start, gameType, reference);
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

        public IEnumerable<TpListModel> GetTpListByTpId(int tpId)
        {
            var key = $"GetTpListByTpId_{tpId}";
            var setting = cache.Get<IEnumerable<TpListModel>>(key);
            if (setting == null)
            {
                var dataTpList = thirdPartyHelper.GetTpListByTpId(tpId);
                if (dataTpList != null)
                {
                    setting = dataTpList.Where(x => x.Status == 1).ToList();
                    cache.Set(key, setting, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return setting;
        }
        
        /// <summary>
        /// 不分SiteId，用WorkerType取回TicketKey
        /// </summary>
        public long GetAllSiteTicketKey(WorkerType workerType, int tpId)
        {
            return thirdPartyHelper.GetSiteTicketKey(0, tpId, workerType);
        }

        /// <summary>
        /// 不分SiteId，用WorkerType更新TicketKey
        /// </summary>
        public int SetAllSiteTicketKey(WorkerType workerType, int tpId, long ticketKey)
        {
            return thirdPartyHelper.SetSiteTicketKey(0, tpId, workerType, ticketKey);
        }
        
    }
}
