using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService _thirdPartyService;
        private readonly IEnumerable<ConfigObject> _configs;

        public ThirdPartyBLL(IServiceProvider provider)
        {
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            _configs = provider.GetRequiredService<IEnumerable<ConfigObject>>();
        }

        public IEnumerable<ConfigObject> GetTpConfigList(string tpName)
        {
            return _configs.Where(i => i.SiteId != 0 && i.ConfigKey == tpName);
        }

        public long GetSiteTicketKey(WorkerType workerType, IThirdPartyBetHelper betHelper)
        {
            return _thirdPartyService.GetSiteTicketKey(betHelper.SiteId, (int)betHelper.Tp, workerType);
        }

        public int SetSiteTicketKey(WorkerType workerType, IThirdPartyBetHelper betHelper, long ticketKey)
        {
            return _thirdPartyService.SetSiteTicketKey(betHelper.SiteId, (int)betHelper.Tp, workerType, ticketKey);
        }

        public int AddTpBetRecordDetail(TpBetRecordDetail detail, DateTime? partitionTime = null)
        {
            return _thirdPartyService.AddTpBetRecordDetail(detail, partitionTime);
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            return _thirdPartyService.GetTpCustInfoWithCache(tpId, tpUserName);
        }

        public IEnumerable<VgamingUnSettle> GetVgamingUnSettleTicket(int SiteId, long TicketKey, int LimitCount = 1000)
        {
            return _thirdPartyService.GetVgamingUnSettleTicket(SiteId, TicketKey, LimitCount);
        }

        public long GetVgamingTicketKey(int SiteId, long TicketKey)
        {
            return _thirdPartyService.GetVgamingTicketKey(SiteId, TicketKey);
        }
    }
}
