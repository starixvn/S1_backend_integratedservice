using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IPG2BLL
{
    BalanceResponse_PG2 GetBalance(CommonRequest_PG2 request);
    Task<BetResponse_PG2> Bet(BetRequest_PG2 request);
    Task<PayoutResponse_PG2> Payout(PayoutRequest_PG2 request);
    Task<SettleResponse_PG2> Settlement(SettlementRequest_PG2 request);
    Task<CancelResponse_PG2> Cancel(CancelRequest_PG2 request);
}