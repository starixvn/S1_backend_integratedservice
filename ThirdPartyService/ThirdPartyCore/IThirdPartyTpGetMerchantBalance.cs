using ThirdPartyCore.Model;
using ThirdPartyCore.Model.TpMerchantBalance.Response;

namespace ThirdPartyCore
{
    public interface IThirdPartyTpGetMerchantBalance
    {
        ApiResult<MerchantBalanceResponse> GetMerchantBalance(TpMerchantInfo tpMerchantInfo, Dictionary<string, string> inputs);
    }
}
