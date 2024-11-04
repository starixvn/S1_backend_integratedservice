using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IEdayBLL
    {
        EdayResponse<EdayGetBalanceResponse> GetBalance(string accountName);
        EdayResponse<EdayBetRequestResponse> BetRequest(EdayBetRequest request);
        EdayResponse<EdayBetRequestResponse> BetRequestCancel(EdayBetRequest request);
        EdayResponse<EdayRoundUpdateResponse> RoundUpdate(EdayRoundUpdateRequest request);
    }
}