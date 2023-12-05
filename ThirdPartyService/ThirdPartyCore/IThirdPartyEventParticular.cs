using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IThirdPartyEventParticular
    {
        ApiResult AddFreeSpinToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
    }
}