using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface INTConnectBLL
{
    NTCResponse<NTCGetBalanceResponse> GetBalance(NTCGetBalanceRequest request);
    NTCResponse<NTCCheckBetResponse> CheckBet(NTCCheckBetRequest request);
    NTCResponse<NTCPlaceBetResponse> PlaceBet(NTCPlaceBetRequest request);
    NTCResponse<NTCSettleBetResponse> SettleBet(NTCSettleBetRequest request);
    NTCResponse<NTCCancelBetResponse> CancelBet(NTCCancelBetRequest request);
}