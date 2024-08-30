using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IPG2BLL
{
    Task<Response_PG2> GetBalance(CommonRequest_PG2 request);
    Task<Response_PG2> Bet(BetRequest_PG2 request);
    Task<Response_PG2> Payout(PayoutRequest_PG2 request);
    Task<Response_PG2> Settlement(SettlementRequest_PG2 request);
    Task<Response_PG2> Cancel(CancelRequest_PG2 request);
}