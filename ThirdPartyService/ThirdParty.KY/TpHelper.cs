using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Web;
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
        private readonly WebProxy? _proxy;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(TpEnum.KY.ToString());

            var tokyoProxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ProxyConfig>("TokyoProxy");

            if (tokyoProxyConfig != null
                && !string.IsNullOrEmpty(tokyoProxyConfig.ProxyHost)
                && tokyoProxyConfig.ProxyPort > 0)
            {
                try
                {
                    _proxy = new WebProxy(tokyoProxyConfig.ProxyHost, tokyoProxyConfig.ProxyPort);
                }
                catch (UriFormatException ex)
                {
                    _proxy = null;
                }
            }
            else
            {
                _proxy = null;
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = config.ApiUrl;
            string Agent = config.CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            string Md5key = config.CurrencyData[tpCustInfo.TpCurrencyId].Md5Key;
            string DesKey = config.CurrencyData[tpCustInfo.TpCurrencyId].DesKey;

            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = currentAt.ToUnixTimeMilliseconds().ToString();
            var timecnv = currentAt.ToString("yyyyMMddHHmmssfff");

            string account = tpCustInfo.TpUserName;
            string orderId = $"{Agent}{timecnv}{account}";
            string ip = inputs.ContainsKey("ClientIP") && !string.IsNullOrEmpty(inputs["ClientIP"]) ? inputs["ClientIP"] : "";
            string lineCode = tpCustInfo.SiteId.ToString();
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");

            var parameters = new Dictionary<string, string>()
            {
                { "s", "0" },
                { "account", account },
                { "money", "0" },
                { "orderid", orderId },
                { "ip", ip },
                { "lineCode", lineCode },
                { "KindID", gameId }
            };

            var sParams = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));
            string eParams = AesEncrypt(sParams, DesKey);
            string postParams = $"agent={Agent}&timestamp={timestamp}&param={eParams}&key={ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()}";

            var result = HttpGetWithProxy(ApiUrl, postParams, _proxy);

            ApiResult oApiResult = new ApiResult();
            if (string.IsNullOrEmpty(result) || !IsJSON(result))
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }
            else
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                ErrorCodeEnum errorMessage = (ErrorCodeEnum)(json.d.code);

                if (json.d.code == (int)ErrorCodeEnum.Success)
                {
                    var uri = new Uri(json.d.url);
                    var forward = json.d.url;
                    string pLang = HttpUtility.ParseQueryString(uri.Query).Get("lang");
                    if (pLang != TpLangId)
                    {
                        forward = forward.Replace($"lang={pLang}", $"lang={TpLangId}");
                    }

                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.d.url;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = errorMessage.ToString();
                    oApiResult.RawErrorCode = json.d.code.ToString();
                    //TODO: test key logs
                    oApiResult.Message = $"Agent: {Agent}, Md5key: {Md5key}, DesKey: {DesKey}, currentAt: {currentAt}, timestamp: {timestamp}, timecnv: {timecnv}";
                }
            }
            return oApiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new()
            {
                ErrorCode = ErrorCode.Success
            };
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
