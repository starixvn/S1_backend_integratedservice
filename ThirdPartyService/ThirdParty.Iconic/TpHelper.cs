using System.Collections.Concurrent;
using System.Text;
using System.Web;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Iconic
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private static ConcurrentDictionary<string, JwtToken> jwtTokens = new ConcurrentDictionary<string, JwtToken>();
        private class ApiErrorResponse
        {
            public Error error { get; set; }
        }

        private class Error
        {
            public int status { get; set; }
            public string message { get; set; }
            public string ip { get; set; }
        }

        private class SignupResult
        {
            public Signup data { get; set; }
        }

        private class Signup
        {
            public string username { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string password = inputs.ContainsKey("password") && !string.IsNullOrEmpty(inputs["password"]) ? inputs["password"] : throw new Exception("password");
            string agent = inputs.ContainsKey($"agent_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"agent_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"agent_{tpCustInfo.TpCurrencyId}"] : throw new Exception($"agent_{tpCustInfo.TpCurrencyId}");
            string token = null;

            if (!jwtTokens.ContainsKey(currency))
            {
                jwtTokens.TryAdd(currency, new JwtToken());
            }

            if (jwtTokens[currency].ExpiredTime <= DateTime.Now)
            {
                string jwtUserName = agent;
                if (string.IsNullOrWhiteSpace(jwtTokens[currency].token))
                {
                    var jwtToken = GetJwtToken(ApiUrl, jwtUserName, password);

                    if (!string.IsNullOrWhiteSpace(jwtToken.token))
                    {
                        jwtTokens[currency] = jwtToken;
                    }
                }
                else
                {
                    Task.Run(() =>
                    {
                        var jwtToken = GetJwtToken(ApiUrl, jwtUserName, password);

                        if (!string.IsNullOrWhiteSpace(jwtToken.token))
                        {
                            var c = jwtTokens[currency];
                            jwtTokens.TryUpdate(currency, jwtToken, c);
                        }
                    });
                }
            }

            token = jwtTokens[currency].token;

            var SignupUrl = $"{ApiUrl}/api/v1/players";

            ApiResult oApiResult = new ApiResult();
            oApiResult.RawMessage = "No Result";
            oApiResult.RawErrorCode = "1";
            oApiResult.ErrorCode = ErrorCode.Fail;

            var param = new
            {
                username = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Authorization", $"Bearer {token}");

            var result = HttpPost(
                SignupUrl,
                JsonConvert.SerializeObject(param),
                "application/json",
                header
            );

            if (IsJSON(result))
            {
                var err = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (err.error != null)
                {
                    oApiResult.RawMessage = err.error.message;
                    oApiResult.RawErrorCode = err.error.status.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
                else
                {
                    var succ = JsonConvert.DeserializeObject<SignupResult>(result);
                    if (succ.data.username == tpCustInfo.TpUserName)
                    {
                        oApiResult.RawMessage = "";
                        oApiResult.RawErrorCode = "";
                        oApiResult.ErrorCode = ErrorCode.Success;
                    }
                }
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception("GameUrl");
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}/{gameId}?lang={TpLangId}&openExternalBrowser=1";
                return oApiResult;
            }

            string currency = tpCustInfo.TpCurrencyId;
            string platformId = inputs.ContainsKey($"platform_{currency}") && !string.IsNullOrEmpty(inputs[$"platform_{currency}"]) ? inputs[$"platform_{currency}"] : throw new Exception($"platform_{currency}");

            string gameType = "39";
            if (gameId.StartsWith("cmf", StringComparison.OrdinalIgnoreCase))
            {
                gameType = "40";
            }
            else if (gameId.StartsWith("cmt", StringComparison.OrdinalIgnoreCase))
            {
                gameType = "40";
            }
            else if (gameId.StartsWith("cm", StringComparison.OrdinalIgnoreCase))
            {
                gameType = "39";
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameType };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}/{gameId}?openExternalBrowser=1&platform={platformId}&token={HttpUtility.UrlEncode(sessionKey)}&lang={TpLangId}";
            return oApiResult;
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                var apiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception();
                var token = inputs.GetValueOrDefault($"token_{tpCustInfo.TpCurrencyId}") ?? throw new Exception();
                var header = new Dictionary<string, string>()
                {
                     { "Authorization", $"Bearer {token}"}
                };

                var url = $"{apiUrl}/api/v1/profile/info/link";
                var sParam = $"id={getBetDetail.TransId}&lang=en";

                var json = HttpGet(url, sParam, "application/x-www-form-urlencoded", header);
                var result = FromJson<DetailResult>(json);

                if (string.IsNullOrEmpty(result.data))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.data,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            catch (Exception e)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {e.Message}");
            }
        }

        public class DetailResult
        {
            public string data { get; set; }
        }

        private JwtToken GetJwtToken(string apiUrl, string username, string password)
        {
            // param
            string url = $"{apiUrl}/login";
            // request
            string r = HttpPost(url, $"username={username}&password={password}", "application/x-www-form-urlencoded");
            var result = JsonConvert.DeserializeObject<JwtToken>(r);
            if (!string.IsNullOrWhiteSpace(result.token))
            {
                result.ExpiredTime = DateTime.Now.AddDays(1);
            }

            return result;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_AT_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("productId"),
                Game = detail.GetStringValue("game"),
                RefNo = detail.GetStringValue("setId"),
                Title = tpGameType.TypeName,
                MatchID = string.Empty,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        public class JwtToken
        {
            public int status { get; set; }
            public string token { get; set; }
            public DateTime ExpiredTime { get; set; }
            public ErrorIconic error { get; set; }
        }
        public class ErrorIconic
        {
            public int status { get; set; }
            public string message { get; set; }
            public string ip { get; set; }
        }
    }
}