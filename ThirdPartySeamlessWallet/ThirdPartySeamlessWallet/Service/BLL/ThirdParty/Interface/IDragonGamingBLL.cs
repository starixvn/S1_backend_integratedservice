using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IDragonGamingBLL
{
    Task<SessionResponse_DragonGaming> GetSession(SessionRequest_DragonGaming request);
    Task<BalanceResponse_DragonGaming> GetBalance(BalanceRequest_DragonGaming request);
    Task<DebitCreditRefundResponse_DragonGaming> Debit(DebitRequest_DragonGaming request);
    Task<DebitCreditRefundResponse_DragonGaming> Credit(CreditRequest_DragonGaming request);
    Task<DebitCreditRefundResponse_DragonGaming> Refund(RefundRequest_DragonGaming request);
}
