using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.KY
{
    public abstract class TpHelper : ThirdPartyHelperBase
    {
        public class LoginResult
        {
            public int error { get; set; }
            public string description { get; set; }
            public string gameURL { get; set; }
        }

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

        private static string GetCurrentTimeFormat()
        {
            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            return currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime);
        }
    }
}
