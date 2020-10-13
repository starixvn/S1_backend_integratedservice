using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            { "MYR", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 200 } },
            { "THB", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 200 } },
            { "CNY", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 200 } },
            { "IDR", new betLimit { maxbet = 200, minbet = 1, mindraw = 1, matchlimit = 200, maxdraw = 200 } }
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

            SignupReq SignUp = new SignupReq();
            SignUp.cert = cert;
            SignUp.agentId = agentId;
            SignUp.userId = tpCustInfo.TpUserName;
            SignUp.currency = tpCustInfo.TpCurrencyId;
            SignUp.betLimit = JsonConvert.SerializeObject(betLimitMapping[tpCustInfo.TpCurrencyId]);

            string InputJson = JsonConvert.SerializeObject(SignUp);
            var result = HttpPost(ApiUrl, InputJson, "application/x-www-form-urlencoded;charset=utf-8");
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SignupResult>(result);
                if (json.status == "0000")
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

            LoginReq Login = new LoginReq();
            Login.cert = cert;
            Login.agentId = agentId;
            Login.userId = tpCustInfo.TpUserName;
            Login.isMobileLogin = (appType == AppType.Desktop) ? "false" : "true";
            Login.externalURL = lobbyUrl;
            Login.gameType = "LIVE";
            Login.platform = "SV388";
            Login.language = TpLangId;

            string InputJson = JsonConvert.SerializeObject(Login);
            var result = HttpPost(ApiUrl, InputJson, "application/x-www-form-urlencoded;charset=utf-8");
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
