using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IKMService
    {
        Task<KMDebitResponse> Debit(KMDebitRequest request);
        void Credit();
        KMBalanceResponse GetBalance(KMBalanceRequest request);
    }
}