using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
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

        private class PGSiteConfig : TpSiteConfig
        {
            public string GameUrl_CNY { get; set; }
            public string GameUrl_THB { get; set; }
            public string secret_key { get; set; }
            public string BetDetailUrl { get; set; }
            public string operator_token { get; set; }
        }

        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, PGSiteConfig> _siteConfigs;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList(14).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PGSiteConfig>(TpEnum.PG.ToString());
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
            var config = _siteConfigs[tpCustInfo.SiteId];

            var operatorToken = config.operator_token;
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var betType = "1";

            var oApiResult = new ApiResult();

            #region SessionKey

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out var token);
            var sessionKeyArr = new[] { key, token };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            #endregion

            var apiUrl = config.ApiUrl;
            var uri = "external-game-launcher/api/v1/GetLaunchURLHTML";
            var clientIp = inputs["ClientIP"];
            var traceId = Guid.NewGuid();

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

            var result = HttpPostClientWithGzip(
                $"{apiUrl}/{uri}?trace_id={traceId}",
                sParam,
                "application/x-www-form-urlencoded"
            );

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = result;

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigs[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var betDetailUrl = config.BetDetailUrl;
            var operatorToken = config.operator_token;
            var secretKey = config.secret_key;
            var uri = $"external/Login/v1/LoginProxy";

            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_PG_Detail_Get", new { getBetDetail.TransId }, DataBase.Report)
                ?? throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");

            var traceId = detail.GetValueOrDefault("TraceId")?.ToString();
            var parentBetId = detail.GetValueOrDefault("ParentBetId").ToString();
            var betId = detail.GetValueOrDefault("BetId").ToString();
            if (string.IsNullOrEmpty(traceId))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }

            var betDetail = new
            {
                operator_token = operatorToken,
                secret_key = secretKey
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(betDetail));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                $"{apiUrl}/{uri}?trace_id={traceId}",
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
                DetailUrl = $"{betDetailUrl}?trace_id={traceId}&t={json.data.operator_session}&psid={parentBetId}&sid={betId}&lang=en&type=operator",
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
            if (!_gameNameList.TryGetValue(gameId, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }
    }
}
