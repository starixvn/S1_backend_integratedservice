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
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(TpEnum.KY.ToString());
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = $"{config.ApiUrl}";
            string Agent = config.Agent;
            var key = ApplyToken(tpCustInfo);

            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : homeUrl;

            string timestamp = GetCurrentTimeFormat();
            var parameters = new Dictionary<string, string>()
            {
                { "token", ApplyToken(tpCustInfo) },
                { "symbol", gameId },
                { "technology", "H5" },
                { "platform", (appType == AppType.Desktop) ? "WEB" : "MOBILE" },
                { "language", TpLangId },
                { "lobbyUrl", lobbyUrl },
                { "cashierUrl", cashierUrl },
                { "playMode", (fun.Equals("1", StringComparison.OrdinalIgnoreCase)) ? "DEMO" : "REAL" },
            };

            string postParams = $"agent={Agent}&timestamp={timestamp}param={parameters}&key={ToMD5(key).ToLower()}";

            var result = HttpGet($"{ApiUrl}channelHandle", postParams, "application/json");

            ApiResult oApiResult = new ApiResult();
            if (!IsJSON(result))
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResult>(result);
            if (json.error == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.gameURL;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.description;
                oApiResult.RawErrorCode = json.error.ToString();
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

        private static string GetCurrentTimeFormat()
        {
            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            return currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime);
        }
    }
}
