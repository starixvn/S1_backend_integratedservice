using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IDragonGamingBLL
{
    Task<CommonResponse_DragonGaming> GetSession(SessionRequest_DragonGaming request);
    Task<CommonResponse_DragonGaming> GetBalance(BalanceRequest_DragonGaming request);
    Task<CommonResponse_DragonGaming> Debit(DebitRequest_DragonGaming request);
    Task<CommonResponse_DragonGaming> Credit(CreditRequest_DragonGaming request);
    Task<CommonResponse_DragonGaming> Refund(RefundRequest_DragonGaming request);
}
