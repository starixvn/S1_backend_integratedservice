using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.Joker
{
    public class TpHelper : ThirdPartyHelperBase
    {
        #region Model

        public class UsrReq
        {
            public string AppID { get; set; }
            public string Hash { get; set; }
            public long Timestamp { get; set; }
        }
        public class LoginReq
        {
            public string token { get; set; }
            public string appID { get; set; }
            public string gameCode { get; set; }
            public string language { get; set; }
            public bool mobile { get; set; }
            public string redirectUrl { get; set; }
        }

        public class DetailResult
        {
            public string Error { get; set; }
            public string Description { get; set; }
            public string Url { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
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
            string gameUrl = string.Empty;
            string appId = string.Empty;
            string secret = string.Empty;

            gameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"])
                     ? inputs["GameUrl"]
                     : throw new Exception("GameUrl is empty");

            if (tpCustInfo.IsTest)
            {
                appId = inputs.ContainsKey($"AppID_{tpCustInfo.TpCurrencyId}_Test") && !string.IsNullOrEmpty(inputs[$"AppID_{tpCustInfo.TpCurrencyId}_Test"])
                      ? inputs[$"AppID_{tpCustInfo.TpCurrencyId}_Test"]
                      : throw new Exception($"AppID_{tpCustInfo.TpCurrencyId}_Test is empty");
                secret = inputs.ContainsKey($"Secret_{tpCustInfo.TpCurrencyId}_Test") && !string.IsNullOrEmpty(inputs[$"Secret_{tpCustInfo.TpCurrencyId}_Test"])
                      ? inputs[$"Secret_{tpCustInfo.TpCurrencyId}_Test"]
                      : throw new Exception($"Secret_{tpCustInfo.TpCurrencyId}_Test is empty");
            }
            else
            {
                appId = inputs.ContainsKey($"AppID_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"AppID_{tpCustInfo.TpCurrencyId}"])
                      ? inputs[$"AppID_{tpCustInfo.TpCurrencyId}"]
                      : throw new Exception($"AppID_{tpCustInfo.TpCurrencyId} is empty");

                secret = inputs.ContainsKey($"Secret_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"Secret_{tpCustInfo.TpCurrencyId}"])
                       ? inputs[$"Secret_{tpCustInfo.TpCurrencyId}"]
                       : throw new Exception($"Secret_{tpCustInfo.TpCurrencyId} is empty");
            }

            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var token = ApplyToken(tpCustInfo);
            var sessionKeyArr = new string[] { token, gameCode };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            var loginReq = new LoginReq
            {
                token = sessionKey,
                appID = appId,
                gameCode = gameCode,
                language = TpLangId,
                mobile = appType != AppType.Desktop,
                redirectUrl = homeUrl
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{gameUrl}?{sParam}"
            };
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
                var appID = inputs.GetValueOrDefault($"AppID_{tpCustInfo.TpCurrencyId}") ?? throw new Exception();
                var secret = inputs.GetValueOrDefault($"Secret_{tpCustInfo.TpCurrencyId}") ?? throw new Exception();
                if (tpCustInfo.IsTest)
                {
                    appID = inputs.GetValueOrDefault($"AppID_{tpCustInfo.TpCurrencyId}_Test") ?? throw new Exception();
                    secret = inputs.GetValueOrDefault($"Secret_{tpCustInfo.TpCurrencyId}_Test") ?? throw new Exception();
                }
                var data = getBetDetail.TpGameTypeOption.Type == 13 ?
                    TpBetRecordProvider.ReportDbQueryFirst("Service_Bettrans_Joker_GameCode_Get", new { getBetDetail.TransId }) :
                    TpBetRecordProvider.ReportDbQueryFirst("Service_BettransFish_Joker_Unsettle_Get", new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId });

                var url = $"{apiUrl}/game-history-url";
                var parameters = new Dictionary<string, string>()
                {
                    { "AppID", appID },
                    { "Username", tpCustInfo.TpUserName },
                    { "GameCode", data.GetValueOrDefault("gamecode")?.ToString()},
                    { "RoundID", getBetDetail.TransId},
                    { "Language","en"},
                    { "Timestamp",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()}
                };

                var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key.ToLower()}={kp.Value}"));
                parameters.Add("Hash", ToMD5($"{sParam}{secret}").ToLower());

                var json = HttpPost(url, ToJson(parameters), "application/json");
                var result = FromJson<DetailResult>(json);

                if (result.Error != "0")
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.Error} | {result.Description}");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.Url,
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

    }
}
