using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IBGBLL
{
    string GetSign(SignParameters_BG signParameters, bool isSignNeedLoginId);

    ResponseModel_BG UserBalance(SeamlessWalletRequest request, UserBalanceParameters_BG parameters);

    Task<ResponseModel_BG> OrderTransfer(SeamlessWalletRequest request, OrderTransferParameters_BG parameters);

    Task<ResponseModel_BG> CalcTransfer(SeamlessWalletRequest request, CalcTransferParameters_BG parameters);

    ResponseModel_BG OrderStatus(SeamlessWalletRequest request, OrderStatusParameters_BG parameters);
}
