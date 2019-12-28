using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL
{
    public class TpCustInfoBLL
    {
        private readonly TpCustInfoService tpCustInfoService;

        public TpCustInfoBLL(TpCustInfoService tpCustInfoService)
        {
            this.tpCustInfoService = tpCustInfoService;
        }
        public void SetTpLastLoginTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastLoginTime(custId, tpId);
        }
        public void SetTpLastTradingTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastTradingTime(custId, tpId);
        }

    }
}
