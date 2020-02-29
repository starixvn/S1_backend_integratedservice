using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        public ThirdPartyBLL(ThirdPartyService thirdPartyService)
        {
            this.thirdPartyService = thirdPartyService;
        }
        public IEnumerable<ThirdPartyModel> GetTpList()
        {
            var tpList = thirdPartyService.GetTpList().ToList();
            var siteTpList = thirdPartyService.GetSiteTpList();
            if (siteTpList != null)
            {
                foreach (var siteTp in siteTpList)
                {
                    tpList.Add(siteTp);
                }
            }

            return tpList;
        }
        public void ToDoAddBetRecord(IEnumerable<BetRecordInfo> betRecordInfos)
        {
            thirdPartyService.ToDoAddBetRecord(betRecordInfos);
        }
        public int SetTicketKey(int TpId, long TicketKey)
        {
            return thirdPartyService.SetTicketKey(TpId, TicketKey);
        }
        public int SiteTpTicketKey(int TpId, int SiteId, long TicketKey)
        {
            return thirdPartyService.SiteTpTicketKey(TpId, SiteId, TicketKey);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            return thirdPartyService.GetTpCurrency(tpId, currencyId);
        }

    }
}
