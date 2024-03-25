using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IYeebetBLL
{
    Task<BalanceResponse_Yeebet> Balance(string username);

    Task<DepositResponse_Yeebet> Deposit(DepositRequest_Yeebet request);

    Task<DeductResponse_Yeebet> Deduct(DeductRequest_Yeebet request);

    Task<RollbackResponse_Yeebet> Rollback(RollbackRequest_Yeebet request);
}
