using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.CQ9
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class LoginReq
        {
            public string account { get; set; }
            public string gamehall { get; set; }
            public string gamecode { get; set; }
            public string gameplat { get; set; }
            public string lang { get; set; }
            public string session { get; set; }
        }
        public class LoginResult
        {
            public Data data { get; set; }
            public Status status { get; set; }
        }
        public class Data
        {
            public string token { get; set; }
            public string url { get; set; }
        }
        public class Status
        {
            public string code { get; set; }
            public string message { get; set; }
            public string datetime { get; set; }
            public string traceCode { get; set; }
        }


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
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var paramToken = $"API_Token_{tpCustInfo.TpCurrencyId}";
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string API_Token = inputs.ContainsKey($"{paramToken}") && !string.IsNullOrEmpty(inputs[$"{paramToken}"]) ? inputs[$"{paramToken}"] : throw new Exception($"{paramToken}");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (!char.IsDigit(gameId[0]))
                {
                    throw new Exception("the free game not support");
                }
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}/{gameId}?language={TpLangId}&token=guest_{tpCustInfo.TpUserName}";
            }
            else
            {
                List<long> testHierarchyCustId = new List<long>();
                if (IsDevelopment == true)
                {
                    testHierarchyCustId.Add(1481);
                }
                else
                {
                    testHierarchyCustId.Add(90);
                    testHierarchyCustId.Add(31909);
                }
                bool isTestHierarchy = testHierarchyCustId.Contains(tpCustInfo.CustId);
                if (isTestHierarchy == true)
                {
                    API_Token = inputs.ContainsKey("API_Token_TEST") && !string.IsNullOrEmpty(inputs["API_Token_TEST"]) ? inputs["API_Token_TEST"] : throw new Exception("API_Token_TEST");
                }

                // Header
                Dictionary<string, string> headerReq = new Dictionary<string, string>();
                headerReq.Add("Authorization", API_Token);
                // Token
                inputs.TryGetValue("Token", out string token);

                var mobileParam = "web";
                if (appType != AppType.Desktop)
                {
                    mobileParam = "mobile";
                }

                // Login params
                LoginReq loginReq = new LoginReq
                {
                    account = tpCustInfo.TpUserName,
                    gamehall = "cq9",
                    gamecode = gameId,
                    gameplat = mobileParam,
                    lang = TpLangId,
                    session = HttpUtility.UrlEncode(token)
                };

                // Call
                var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
                var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
                var result = HttpPost(
                    $"{ApiUrl}/gameboy/player/sw/gamelink",
                    sParam,
                    "application/x-www-form-urlencoded",
                    headerReq
                );

                // Response
                var tpData = JsonConvert.DeserializeObject<LoginResult>(result);
                if (tpData.status.code == "0")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = tpData.data.url;
                }
                else
                {
                    oApiResult.RawMessage = tpData.status.message;
                    oApiResult.RawErrorCode = tpData.status.code;
                    oApiResult.Message = $"{tpData.status.code}-{tpData.status.message}";
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
