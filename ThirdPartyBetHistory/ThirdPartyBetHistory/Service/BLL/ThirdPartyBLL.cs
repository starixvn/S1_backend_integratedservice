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
            return thirdPartyService.GetTpList();
        }
        public void ToDoAddBetRecord(IEnumerable<BetRecordInfo> betRecordInfos)
        {
            thirdPartyService.ToDoAddBetRecord(betRecordInfos);
        }
        public int SetTicketKey(int TpId, long TicketKey)
        {
            return thirdPartyService.SetTicketKey(TpId, TicketKey);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            return thirdPartyService.GetTpCurrency(tpId, currencyId);
        }

    }
}
