using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IThirdPartyParticular
    {
        ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        //ApiResult<TransferResult> BonusTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal bonus, string transID, string thirdPartyTransID);
    }
}
