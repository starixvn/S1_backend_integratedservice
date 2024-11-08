using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IKMService
    {
        bool CheckCredentials(string clientId, string clientSecret);
        bool CheckCredentialsBySiteName(string clientId, string clientSecret, string siteName);
        Task<KMDebitResponse> Debit(KMDebitRequest request);
        Task<KMCreditResponse> Credit(KMCreditRequest request);
        KMBalanceResponse GetBalance(KMBalanceRequest request);
        Task<KMRewardResponse> Reward(KMRewardRequest request);
    }
}