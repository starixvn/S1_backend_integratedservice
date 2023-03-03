using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IBGBLL
{
    Task<ResponseModel_BG> Balance(SeamlessWalletRequest request, BalanceParameters_BG parameters);
}
