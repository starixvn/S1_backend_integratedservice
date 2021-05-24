using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IKMService
    {
        bool CheckCredentials(string clientId, string clientSecret);
        Task<KMDebitResponse> Debit(KMDebitRequest request);
        Task<KMCreditResponse> Credit(KMCreditRequest request);
        KMBalanceResponse GetBalance(KMBalanceRequest request);
    }
}