using System.Collections.Generic;
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
            return commonService.GetTpCurrencyList(tpId);
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return commonService.GetWagerSummary(spName, beginTime, endTime);
        }
    }
}
