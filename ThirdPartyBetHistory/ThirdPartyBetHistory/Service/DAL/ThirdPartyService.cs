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
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly ICacheHelper _redisHelper;

        public ThirdPartyService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            var tpCustInfo = _redisHelper.TryGetOrCreate(
                $"TpCustInfo:{tpId}:{tpUserName}",
                TimeSpan.FromMinutes(1),
               () => GetTpCustInfo(tpId, tpUserName));
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

        public IEnumerable<VgamingUnSettle> GetVgamingUnSettleTicket(long TicketKey, int LimitCount)
        {
            return _reportDbClient.Query<VgamingUnSettle>("Service_SBSW_Vgaming_Unsettle_Get_V3",
                new
                {
                    TicketKey,
                    LimitCount,
                }, 120);
        }

        public long GetVgamingTicketKey(long TicketKey)
        {
            var parameters = new { TicketKey };
            return _reportDbClient.QueryFirst<long>("Service_SBSW_Vgaming_TicketKey_Get_V2", parameters);
        }
    }
}
