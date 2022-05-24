using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBOBLL
    {
        SBOBalanceResponse GetBalance(SBOBalanceRequest request);
        Task<SBODeductResponse> Deduct(SBODeductRequest request);
        Task<SBOSettleResponse> Settle(SBOSettleRequest request);
        Task<SBOGetBetStatusResponse> GetBetStatus(SBOGetBetStatusRequest request);
    }
}