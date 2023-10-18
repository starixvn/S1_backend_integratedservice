using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IThirdPartyBetLimitParticular
    {
        ApiResult SetBetLimit(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, string betSetting);
    }
}
