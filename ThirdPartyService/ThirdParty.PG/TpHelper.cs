using System.Text;
using System.Web;

using HtmlAgilityPack;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class DetailData
        {
            public string operator_session { get; set; }
        }
        public class DetailResponse
        {
            public DetailData data { get; set; }
        }

        private readonly IReadOnlyDictionary<string, string> GameNameList;

        public TpHelper()
        {
            GameNameList = TpBetRecordProvider.GetGameCasinoList(14).ToDictionary(d => d.AppId, d => d.Name);
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            var operatorToken = inputs.ContainsKey("operator_token") && !string.IsNullOrEmpty(inputs["operator_token"]) ? inputs["operator_token"] : throw new Exception("operator_token");
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var clientIp = inputs["ClientIP"];
            var traceId = Guid.NewGuid();
            var betType = "1";
            var uri = "external-game-launcher/api/v1/GetLaunchURLHTML";

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out var token);
            var sessionKeyArr = new[] { key, token };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            var extraArgs = new
            {
                btt = betType,
                ops = HttpUtility.UrlEncode(sessionKey),
                l = TpLangId
            };
            var dicExtraArgs = FromJson<Dictionary<string, string>>(ToJson(extraArgs));
            var sParamExtraArgs = string.Join("&", dicExtraArgs.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var getLaunchUrl = new
            {
                operator_token = operatorToken,
                path = HttpUtility.UrlEncode($"/{gameId}/index.html"),
                extra_args = HttpUtility.UrlEncode(sParamExtraArgs),
                url_type = "game-entry",
                client_ip = clientIp
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(getLaunchUrl));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                $"{apiUrl}/{uri}?trace_id={traceId}",
                sParam,
                "application/x-www-form-urlencoded"
            );

            var oApiResult = new ApiResult();

            var doc = new HtmlDocument();
            doc.LoadHtml(result);
            var metaTag = doc.DocumentNode.SelectSingleNode("//meta[@http-equiv='refresh']");
            if (metaTag != null)
            {
                var contentAttribute = metaTag.GetAttributeValue("content", "");
                var urlStartIndex = contentAttribute.IndexOf("URL=") + 4;
                var url = contentAttribute.Substring(urlStartIndex);

                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = url;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string BetDetailUrl = inputs.ContainsKey("BetDetailUrl") && !string.IsNullOrEmpty(inputs["BetDetailUrl"]) ? inputs["BetDetailUrl"] : throw new Exception("BetDetailUrl");
            string operator_token = inputs.ContainsKey("operator_token") && !string.IsNullOrEmpty(inputs["operator_token"]) ? inputs["operator_token"] : throw new Exception("operator_token");
            string secret_key = inputs.ContainsKey("secret_key") && !string.IsNullOrEmpty(inputs["secret_key"]) ? inputs["secret_key"] : throw new Exception("secret_key");
            string Uri = $"external/Login/v1/LoginProxy";

            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_PG_Detail_Get", new { getBetDetail.TransId }, DataBase.Report);

            string TraceId = detail.GetValueOrDefault("TraceId")?.ToString();
            string ParentBetId = detail.GetValueOrDefault("ParentBetId").ToString();
            string BetId = detail.GetValueOrDefault("BetId").ToString();
            if (string.IsNullOrEmpty(TraceId))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }

            var BetDetail = new
            {
                operator_token = operator_token,
                secret_key = secret_key
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(BetDetail));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                $"{ApiUrl}/{Uri}?trace_id={TraceId}",
                sParam,
                "application/x-www-form-urlencoded"
            );

            if (!IsJSON(result))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result}");
            }

            var json = JsonConvert.DeserializeObject<DetailResponse>(result);
            if (string.IsNullOrEmpty(json.data.operator_session))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail error.");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = $"{BetDetailUrl}?trace_id={TraceId}&t={json.data.operator_session}&psid={ParentBetId}&sid={BetId}&lang=en&type=operator",
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQuery("Service_BettransRng_PG_Detail_GetAll", new { betRecordInfo.TransId }, DataBase.Main);
            var gameCode = detail.GetFirstStringValue("GameId");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detail.GetFirstStringValue("ParentBetId"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetFirstStringValue("ParentBetId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        private string GetGameName(string gameId)
        {
            if (!GameNameList.TryGetValue(gameId, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }
    }
}
