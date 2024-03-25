using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore
{
    public interface IThirdPartyHelper
    {
        ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs);
        ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID);
        ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID);
        TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        event InsertLogEventHandler InsertLog;
        bool isHeathly { set; get; }
    }
}
