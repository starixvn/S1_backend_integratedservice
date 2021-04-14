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
    }
}
