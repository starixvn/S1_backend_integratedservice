using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IEVOBLL
    {
        Task<StandardResponse> PromoPayout(PromoData_EVO request, string authToken);
    }
}
