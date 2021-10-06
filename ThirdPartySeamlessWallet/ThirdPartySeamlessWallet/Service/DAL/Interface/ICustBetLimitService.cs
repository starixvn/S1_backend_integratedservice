using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL.Interface
{
    public interface ICustBetLimitService
    {
        CustBetLimit GetCustBetLimit(int custId);
    }
}