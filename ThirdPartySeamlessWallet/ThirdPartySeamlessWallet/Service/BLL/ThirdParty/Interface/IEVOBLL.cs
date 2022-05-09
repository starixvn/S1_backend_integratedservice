using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IEVOBLL
    {
        public bool CheckRequest(PromoData_EVO request, out EVOStatus status);
        Task<StandardResponse> PromoPayout(PromoData_EVO request, int tpId, int tpGameType);
    }
}
