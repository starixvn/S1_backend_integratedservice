using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService _thirdPartyService;
        public ThirdPartyBLL(IServiceProvider provider)
        {
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
        }
        public IEnumerable<ThirdPartyModel> GetTpList()
        {
            var tpList = _thirdPartyService.GetTpList().ToList();
            var siteTpList = _thirdPartyService.GetSiteTpList();
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
            _thirdPartyService.ToDoAddBetRecord(betRecordInfos);
        }
        public int SetTicketKey(int TpId, long TicketKey)
        {
            return _thirdPartyService.SetTicketKey(TpId, TicketKey);
        }
        public int SiteTpTicketKey(int TpId, int SiteId, long TicketKey)
        {
            return _thirdPartyService.SiteTpTicketKey(TpId, SiteId, TicketKey);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            return _thirdPartyService.GetTpCurrency(tpId, currencyId);
        }

        public long GetSiteTicketKey(WorkerType workerType, IThirdPartyBetHelper betHelper)
        {
            return _thirdPartyService.GetSiteTicketKey(betHelper.SiteId, (int)betHelper.Tp, workerType);
        }

        public int SetSiteTicketKey(WorkerType workerType, IThirdPartyBetHelper betHelper, long ticketKey)
        {
            return _thirdPartyService.SetSiteTicketKey(betHelper.SiteId, (int)betHelper.Tp, workerType, ticketKey);
        }

        public int AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            return _thirdPartyService.AddTpBetRecordDetail(detail);
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            return _thirdPartyService.GetTpCustInfoWithCache(tpId, tpUserName);
        }
    }
}
