using System.Data;

using Dapper;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class ThirdPartyService
    {
        private readonly IMemoryCache _cache;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public ThirdPartyService(IServiceProvider serviceProvider)
        {
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            var key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = _cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    _cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return tpCustInfo;
        }

        private TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            var parameters = new DynamicParameters(new { TpId, TpUserName });
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", parameters);
        }

        public IEnumerable<UnSettle> GetTpBetRecordReSettleTicket(Tp tp, int siteId, IEnumerable<SettlementType> settlementTypeList, DateTime startTime, DateTime endTime)
        {
            return _reportDbClient.Query<UnSettle>("Service_TpBetRecord_ReSettle_Get",
                new
                {
                    _TpId = (int)tp,
                    _SiteId = siteId,
                    SettlementTypeList = JsonConvert.SerializeObject(settlementTypeList.Select(i => (int)i)),
                    settlementTypeList,
                    StartTime = startTime,
                    EndTime = endTime
                }, 120);
        }

        public long GetSiteTicketKey(int SiteId, int TpId, WorkerType workerType)
        {
            var parameters = new { SiteId, TpId, WorkerType = (int)workerType };
            return _mainDbClient.QueryFirst<long>("Service_SiteTicketKey_Get", parameters);
        }

        public int SetSiteTicketKey(int SiteId, int TpId, WorkerType workerType, long TicketKey)
        {
            var parameters = new { SiteId, TpId, WorkerType = (int)workerType, TicketKey };
            return _mainDbClient.Execute("Service_SiteTicketKey_Set", parameters);
        }

        public int AddTpBetRecordDetail(TpBetRecordDetail detail, DateTime? partitionTime = null)
        {
            var props = new DynamicParameters();
            props.Add("TpId", detail.TpId);
            props.Add("GameType", detail.GameType);
            props.Add("TransId", detail.TransId);
            props.Add("CurrencyId", detail.CurrencyId);
            props.Add("BinaryDetail", detail.BinaryDetail);
            props.Add("SettledTime", detail.SettledTime);
            props.Add("PartitionTime", partitionTime);
            return _mainDbClient.Execute("Service_TpBetRecordDetail_Ins_V3", props);
        }
    }
}
