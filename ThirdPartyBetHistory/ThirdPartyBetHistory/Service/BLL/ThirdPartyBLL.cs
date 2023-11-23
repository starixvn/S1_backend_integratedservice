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

        /// <summary>
        /// 分SiteId與WorkerType取回TicketKey
        /// </summary>
        public long GetSiteTicketKey(WorkerType workerType, IThirdPartyBetHelper betHelper)
        {
            return _thirdPartyService.GetSiteTicketKey(betHelper.SiteId, (int)betHelper.Tp, workerType);
        }

        /// <summary>
        /// 分SiteId與WorkerType更新TicketKey
        /// </summary>
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

        #region SBSW Vgaming

        /// <summary>
        /// 取得SBSW Vgaming五分鐘沒收到派彩的下注清單
        /// </summary>
        public IEnumerable<VgamingUnSettle> GetVgamingUnSettleTicket(long TicketKey, int LimitCount = 1000)
        {
            return _thirdPartyService.GetVgamingUnSettleTicket(TicketKey, LimitCount);
        }

        /// <summary>
        /// 取得Vgaming下一次要檢查的TicketKey
        /// </summary>
        /// <param name="TicketKey"></param>
        /// <returns></returns>
        public long GetVgamingTicketKey(long TicketKey)
        {
            return _thirdPartyService.GetVgamingTicketKey(TicketKey);
        }

        #endregion

        #region HaoLong

        /// <summary>
        /// 取得HaoLong尚未派彩的下注清單
        /// </summary>
        public IEnumerable<HaoLongUnSettle> GetHaoLongUnSettleTicket(long TicketKey, int LimitCount = 1000)
        {
            return _thirdPartyService.GetHaoLongUnSettleTicket(TicketKey, LimitCount);
        }

        /// <summary>
        /// 取得HaoLong下一次要檢查的TicketKey
        /// </summary>
        /// <param name="TicketKey"></param>
        /// <returns></returns>
        public long GetHaoLongTicketKey(long TicketKey)
        {
            return _thirdPartyService.GetHaoLongTicketKey(TicketKey);
        }

        #endregion

        /// <summary>
        /// 不分SiteId，用WorkerType取回TicketKey
        /// </summary>
        public long GetAllSiteTicketKey(WorkerType workerType, int tpId)
        {
            return _thirdPartyService.GetSiteTicketKey(0, tpId, workerType);
        }

        /// <summary>
        /// 不分SiteId，用WorkerType更新TicketKey
        /// </summary>
        public int SetAllSiteTicketKey(WorkerType workerType, int tpId, long ticketKey)
        {
            return _thirdPartyService.SetSiteTicketKey(0, tpId, workerType, ticketKey);
        }
    }
}
