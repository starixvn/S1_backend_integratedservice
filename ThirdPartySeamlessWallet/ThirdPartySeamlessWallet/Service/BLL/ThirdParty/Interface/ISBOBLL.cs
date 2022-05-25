using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBOBLL
    {
        SBOBalanceResponse GetBalance(SBOBalanceRequest request);
        Task<SBODeductResponse> Deduct(SBODeductRequest request);
        Task<SBOSettleResponse> Settle(SBOSettleRequest request);
        Task<SBOCancelResponse> Cancel(SBOCancelRequest request);
        Task<SBORollbackResponse> Rollback(SBORollbackRequest request);
        Task<SBOGetBetStatusResponse> GetBetStatus(SBOGetBetStatusRequest request);
        Task<SBOLiveCoinTransactionResponse> LiveCoinTransaction(SBOLiveCoinTransactionRequest request);
    }
}