using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.KY
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class LoginResult
        {
            public int error { get; set; }
            public string description { get; set; }
            public string gameURL { get; set; }
        }

        private readonly Dictionary<int, KYConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(TpEnum.KY.ToString());
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = config.ApiUrl;
            string GameUrl = config.GameUrl;
            string SecureLogin = config.SecureLogin;
            string SecureKey = config.SecureKey;
            if (tpCustInfo.MemberType == CustomerMemberTypeEnum.LiveStreaming)
            {
                ApiUrl = config.ApiUrl_Stream;
                SecureLogin = config.SecureLogin_Stream;
                SecureKey = config.SecureKey_Stream;
            }

            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : homeUrl;

            string Uri = "/v3/http/CasinoGameAPI/game/url";
            var parameters = new Dictionary<string, string>()
            {
                {"secureLogin", SecureLogin },
                { "token", ApplyToken(tpCustInfo) },
                { "symbol", gameId },
                { "technology", "H5" },
                { "platform", (appType == AppType.Desktop) ? "WEB" : "MOBILE" },
                { "language", TpLangId },
                { "lobbyUrl", lobbyUrl },
                { "cashierUrl", cashierUrl },
                { "playMode", (fun.Equals("1", StringComparison.OrdinalIgnoreCase)) ? "DEMO" : "REAL" },
                { "stylename", SecureLogin },
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{SecureKey}")}";
            var result = HttpPost($"{ApiUrl}{Uri}", sParam, "application/x-www-form-urlencoded");

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
    }
}
