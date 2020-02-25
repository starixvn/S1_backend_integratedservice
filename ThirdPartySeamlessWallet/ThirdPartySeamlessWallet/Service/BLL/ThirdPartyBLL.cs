using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;

        public ThirdPartyBLL(ThirdPartyService thirdPartyService)
        {
            this.thirdPartyService = thirdPartyService;
        }

        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            return thirdPartyService.GetThirdPartySetting(tpId);
        }
    }
}
