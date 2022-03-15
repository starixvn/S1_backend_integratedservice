using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IAMBBLL
    {
        Task<AMBSlotGetBalanceResponse> SlotGetBalance(ANBSlotGetBalanceRequest request);
        Task<AMBSlotBetResponse> SlotBet(AMBSlotBetRequest request);
        Task<AMBSlotPayoutResponse> SlotPayout(AMBSlotPayoutRequest request);
        Task<AMBSlotCancelResponse> SlotCancel(AMBSlotCancelRequest request);
    }
}
