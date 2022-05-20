using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBOBLL
    {
        SBOBalanceResponse GetBalance(SBOBalanceRequest request);
    }
}