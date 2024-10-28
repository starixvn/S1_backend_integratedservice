using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.KY
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, KYConfig> _siteConfigDic;
        private const string _formatOfTime = "yyyyMMddHHmmssfff";
        private const int _utc = -4;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(TpEnum.KY.ToString());
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var currentAt = new DateTimeOffset(DateTime.UtcNow);

            string ApiUrl = config.ApiUrl;
            string Agent = config.Agent;
            string OperateSubtype = config.OperateSubtype;
            string timestamp = currentAt.ToUnixTimeMilliseconds().ToString();
            var Md5key = config.Md5Key;
            var DesKey = config.DesKey;

            string account = inputs.ContainsKey("account") && !string.IsNullOrEmpty(inputs["account"]) ? inputs["account"] : "";
            string money = inputs.ContainsKey("money") && !string.IsNullOrEmpty(inputs["money"]) ? inputs["money"] : "";
            // TODO: How to not exceed 100 characters?
            string orderId = $"{Agent}{currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime)}{account}";
            string ip = inputs.ContainsKey("ip") && !string.IsNullOrEmpty(inputs["ip"]) ? inputs["ip"] : "";
            string lineCode = inputs.ContainsKey("lineCode") && !string.IsNullOrEmpty(inputs["lineCode"]) ? inputs["lineCode"] : "";
            string kindId = inputs.ContainsKey("kindId") && !string.IsNullOrEmpty(inputs["kindId"]) ? inputs["kindId"] : "0";

            var parameters = new Dictionary<string, string>()
            {
                { "s", OperateSubtype },
                { "account", account },
                { "money", money },
                { "orderid", orderId },
                { "ip", ip },
                { "lineCode", lineCode },
                { "KindID", kindId }
            };

            var sParams = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));
            string eParams = AesEncrypt(sParams, DesKey);
            string postParams = $"agent={Agent}&timestamp={timestamp}param={sParams}&key={ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()}";

            var result = HttpGet($"{ApiUrl}channelHandle", postParams, "application/json");

            ApiResult oApiResult = new ApiResult();
            if (string.IsNullOrEmpty(result) || !IsJSON(result))
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResult>(result);
            ErrorCodeEnum errorMessage = (ErrorCodeEnum)(json.DataResult.Code);

            if (errorMessage == ErrorCodeEnum.Success)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.DataResult.Url;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = errorMessage.ToString();
                oApiResult.RawErrorCode = ((int)errorMessage).ToString();
            }

            return oApiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
