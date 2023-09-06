using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IMGBLL
    {
        MGLoginResponse Login(MGLoginRequest request);
        MGGetBalanceResponse GetBalance(MGGetBalanceRequest request);
        Task<MGUpdatebalanceResponse> UpdateBalance(MGUpdatebalanceRequest request);
        Task<MGRollbackResponse> Rollback(MGRollbackeRequest request);
    }
}