using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service
{
    public interface IThirdPartyBetHelper
    {
        int SiteId { set; get; }
        Tp Tp { set; get; }
        string TpName { get; }
        SabaConfig SabaConfig { set; get; }
        Task UpdateBetRecordByTransId(WorkerType workerType, DateTime startTime, DateTime endTime, IEnumerable<SettlementType> settlementTypeList);             
        Task<long> AddTpBetRecordDetail(WorkerType workerType, long siteTicketKey);
        Task ResubmitProcess(VgamingUnSettle unSettle);
    }
}
