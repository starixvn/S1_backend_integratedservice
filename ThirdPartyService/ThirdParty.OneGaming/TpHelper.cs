using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.OneGaming
{
    public class TpHelper : ThirdPartyHelperBase
    {

        private class ApiResponse
        {
            public string status_code { get; set; }
            public string message { get; set; }
            public string error_code { get; set; }
        }

        private class LoginResponse : ApiResponse
        {
            public string player { get; set; }
            public string currency { get; set; }
            public string balance { get; set; }
            public string LobbyUrl { get; set; }
        }

        private class SignupResponse : ApiResponse
        {
            public string player { get; set; }
            public string currency { get; set; }
            public string balance { get; set; }
            public string LobbyUrl { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentCode = inputs.ContainsKey($"agent_code_{currency}") && !string.IsNullOrEmpty(inputs[$"agent_code_{currency}"]) ? inputs[$"agent_code_{currency}"] : throw new Exception($"agent_code_{currency}");
            string secret_key = inputs.ContainsKey($"secret_key_{currency}") && !string.IsNullOrEmpty(inputs[$"secret_key_{currency}"]) ? inputs[$"secret_key_{currency}"] : throw new Exception($"secret_key_{currency}");
            ApiUrl = $"{ApiUrl}/registerPlayer";

            var param = new
            {
                agent_code = agentCode,
                player_id = tpCustInfo.TpUserName,
                username = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd,
                bcode = HMACSHA256($"{agentCode}{tpCustInfo.TpUserName}{tpCustInfo.TpUserName}{tpCustInfo.TpPwd}", secret_key)
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(param),
                "application/json"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResponse>(result);
                if (json.status_code == "0")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else if (json.status_code == "303")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code ?? json.status_code;
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
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string agentCode = inputs.ContainsKey($"agent_code_{currency}") && !string.IsNullOrEmpty(inputs[$"agent_code_{currency}"]) ? inputs[$"agent_code_{currency}"] : throw new Exception($"agent_code_{currency}");
            string secret_key = inputs.ContainsKey($"secret_key_{currency}") && !string.IsNullOrEmpty(inputs[$"secret_key_{currency}"]) ? inputs[$"secret_key_{currency}"] : throw new Exception($"secret_key_{currency}");

            if (appType == AppType.Desktop)
            {
                ApiUrl = $"{ApiUrl}/gameLobby_URL";
            }
            else
            {
                ApiUrl = $"{ApiUrl}/gameLobby_m_URL";
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var param = new
            {
                agent_code = agentCode,
                player_id = tpCustInfo.TpUserName,
                l_token = HttpUtility.UrlEncode(sessionKey),
                bcode = HMACSHA256($"{agentCode}{tpCustInfo.TpUserName}{HttpUtility.UrlEncode(sessionKey)}", secret_key)
            };

            var result = HttpPost(
                ApiUrl,
                JsonConvert.SerializeObject(param),
                "application/json"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResponse>(result);
                if (json.status_code == "0")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.LobbyUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code ?? json.status_code;
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

        private string HMACSHA256(string message, string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
            }
        }
    }
}
