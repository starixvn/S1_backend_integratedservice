using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IBGBLL
{
    bool GetSecretKeyForCheckSign(string loginId, out string secretKey, out string message);

    ResponseModel_BG UserBalance(SeamlessWalletRequest request, UserBalanceParameters_BG parameters);

    Task<ResponseModel_BG> OrderTransfer(SeamlessWalletRequest request, OrderTransferParameters_BG parameters);

    Task<ResponseModel_BG> CalcTransfer(SeamlessWalletRequest request, CalcTransferParameters_BG parameters);

    ResponseModel_BG OrderStatus(SeamlessWalletRequest request, OrderStatusParameters_BG parameters);
}
