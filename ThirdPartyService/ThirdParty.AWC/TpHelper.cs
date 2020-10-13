using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.AWC
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class SignupReq
        {
            public string cert { get; set; }
            public string agentId { get; set; }
            public string userId { get; set; }
            public string currency { get; set; }
            public string betLimit { get; set; }
            public string language { get; set; }
        }

        private class SignupResult
        {
            public string status { get; set; }
            public string desc { get; set; }
        }

        public class LoginReq
        {
            public string cert { get; set; }
            public string agentId { get; set; }
            public string userId { get; set; }
            public string isMobileLogin { get; set; }
            public string externalURL { get; set; }
            //public string gameForbidden { get; set; }
            public string gameType { get; set; }
            public string platform { get; set; }
            public string language { get; set; }
        }

        private class LoginResult
        {
            public string status { get; set; }
            public string url { get; set; }
        }

        class betLimit
        {
            public int maxbet { get; set; }
            public int minbet { get; set; }
            public int mindraw { get; set; }
            public int matchlimit { get; set; }
            public int maxdraw { get; set; }
        }

        Dictionary<string, betLimit> betLimitMapping = new Dictionary<string, betLimit>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            { "MYR", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 100 } },
            { "THB", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 100 } },
            { "CNY", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 100 } },
            { "IDR", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 100 } }
        };

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception("cert");
            string agentId = inputs.ContainsKey("agentId") && !string.IsNullOrEmpty(inputs["agentId"]) ? inputs["agentId"] : throw new Exception("agentId");
            ApiUrl = $"{ApiUrl}/wallet/createMember";

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            SignupReq SignUp = new SignupReq
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                betLimit = JsonConvert.SerializeObject(new { SV388 = new { LIVE = betLimitMapping[tpCustInfo.TpCurrencyId] } }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SignupResult>(result);
                if (json.status == "0000")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else if (json.status == "1001")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.desc;
                    oApiResult.RawErrorCode = json.status;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception("cert");
            string agentId = inputs.ContainsKey("agentId") && !string.IsNullOrEmpty(inputs["agentId"]) ? inputs["agentId"] : throw new Exception("agentId");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "LobbyUrl";
            ApiUrl = $"{ApiUrl}/wallet/login";


            LoginReq Login = new LoginReq
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                isMobileLogin = (appType == AppType.Desktop) ? "false" : "true",
                externalURL = lobbyUrl,
                gameType = "LIVE",
                platform = "SV388",
                language = TpLangId,
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Login));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                if (json.status == "0000")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.url;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.status;
                    oApiResult.RawErrorCode = json.status;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
