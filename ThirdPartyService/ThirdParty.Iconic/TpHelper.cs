using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Iconic
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private static JwtToken jwtToken1 = null;

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

        private class JwtResponse
        {
            public Data data { get; set; }
            public string token { get; set; }
        }

        private class Data
        {
            public int id { get; set; }
            public string name { get; set; }
            public int userId { get; set; }
            public string username { get; set; }
            public string status { get; set; }
            public string secureToken { get; set; }

        }

        private class JwtToken
        {
            public int status { get; set; }
            public string token { get; set; }
            public DateTime ExpiredTime { get; set; }
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
            string currency = GetTpCurrency(tpCustInfo.TpCurrencyId);
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string password = inputs.ContainsKey("password") && !string.IsNullOrEmpty(inputs["password"]) ? inputs["password"] : throw new Exception("password");
            var SignupUrl = $"{ApiUrl}/api/v1/players";

            ApiResult oApiResult = new ApiResult();
            oApiResult.RawMessage = "No Result";
            oApiResult.RawErrorCode = "1";
            oApiResult.ErrorCode = ErrorCode.Fail;

            if (jwtToken1 == null || jwtToken1?.ExpiredTime < DateTime.Now)
                jwtToken1 = GetJwtTokenWithCache(tpCustInfo.TpId, ApiUrl, currency, password);

            if (jwtToken1 != null)
            {
                var param = new
                {
                    username = tpCustInfo.TpUserName,
                    currency = currency
                };

                var dicP = FromJson<Dictionary<string, string>>(ToJson(param));
                var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("Authorization", $"Bearer {jwtToken1.token}");

                var result = HttpPost(
                    SignupUrl,
                    //sParam,
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
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string currency = GetTpCurrency(tpCustInfo.TpCurrencyId);
            string platformId = inputs.ContainsKey($"platform_{currency}") && !string.IsNullOrEmpty(inputs[$"platform_{currency}"]) ? inputs[$"platform_{currency}"] : throw new Exception($"platform_{currency}");
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception("GameUrl");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}/{gameId}?openExternalBrowser=1&platform={platformId}&token={HttpUtility.UrlEncode(sessionKey)}&lang={TpLangId}";
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        private JwtToken GetJwtTokenWithCache(int tpId, string url, string currency, string password)
        {
            var jwtCheck = GetJwtToken(url, currency, password);
            //string key = $"JwtToken_{tpId}";
            //var jwtCheck = cache.Get<JwtToken>(key);
            //if (jwtCheck == null)
            //{
            //    jwtCheck = GetJwtToken(url, currency, password);
            //    if (jwtCheck.status == 200)
            //    {
            //        cache.Set(key, jwtCheck, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(86400)));
            //    }
            //    else
            //    {
            //        jwtCheck = null;
            //    }
            //}

            return jwtCheck;
        }

        private JwtToken GetJwtToken(string url, string currency, string password)
        {
            url = $"{url}/login";

            var param = new
            {
                username = $"novax{currency}",
                password = password
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                url,
                //sParam,
                JsonConvert.SerializeObject(param),
                "application/json"
            );

            JwtToken resp = new JwtToken
            {
                status = 400,
                token = ""
            };

            if (IsJSON(result))
            {
                var err = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (err.error != null)
                {
                    resp.status = err.error.status;
                }
                else
                {
                    var succ = JsonConvert.DeserializeObject<JwtResponse>(result);
                    resp.status = 200;
                    resp.token = HttpUtility.UrlEncode(succ.token);
                    resp.ExpiredTime = DateTime.Now.AddDays(1);/*Expired Time*/
                }
            }

            return resp;
        }

        private string GetTpCurrency(string currency)
        {
            if (currency == "VND")
            {
                currency = $"{currency}1000";
            }
            else if (currency == "IDR")
            {
                currency = $"{currency}1000";
            }
            return currency;
        }
    }
}