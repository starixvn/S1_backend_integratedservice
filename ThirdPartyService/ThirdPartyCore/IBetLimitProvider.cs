using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IBetLimitProvider
    {
        CustBetLimit GetCustBetLimit(int custId);
    }
}