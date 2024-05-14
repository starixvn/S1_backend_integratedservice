using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IThirdPartyEventParticular
    {
        ApiResult AddPrizeToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);

        ApiResult CreateBonusCode(int siteId,Dictionary<string, string> inputs);
    }
}