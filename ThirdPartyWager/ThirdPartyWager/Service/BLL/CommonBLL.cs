using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class CommonBLL
    {
        private readonly CommonService commonService;

        public CommonBLL(CommonService commonService)
        {
            this.commonService = commonService;
        }
        public IEnumerable<ServerInfo> GetSiteList()
        {
            return commonService.GetSiteList();
        }

        public IEnumerable<TpCurrency> GetTpCurrencyList(int tpId)
        {
            return commonService.GetTpCurrencyList(tpId).Where(x => x.IsSubCurrency == 0);
        }
        public IEnumerable<int> GetSiteByTpGameType(int gameType)
        {
            return commonService.GetSiteByTpGameType(gameType);
        }
        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return commonService.GetWagerSummary(spName, beginTime, endTime);
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummaryBySiteIds(string spName, long beginTime, long endTime, string siteIds)
        {
            return commonService.GetWagerSummaryBySiteIds(spName, beginTime, endTime, siteIds);
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummaryBySiteId(string spName, long beginTime, long endTime, int siteId)
        {
            return commonService.GetWagerSummaryBySiteId(spName, beginTime, endTime, siteId);
        }

        public Task<IEnumerable<FincanceDailySummary>> GetBettransSummary(string spName, long beginTime, long endTime)
        {
            return commonService.GetBettransSummary(spName, beginTime, endTime);
        }

        public int InsertTpWagerRedo(TpWagerRedo redo)
        {
            return commonService.InsertTpWagerRedo(redo);
        }

        public IEnumerable<TpWagerRedo> GetTpWagerRedo(int tpId)
        {
            return commonService.GetTpWagerRedo(tpId);
        }

        public int DeleteTpWagerRedo(TpWagerRedo redo)
        {
            return commonService.DeleteTpWagerRedo(redo);
        }
    }
}
