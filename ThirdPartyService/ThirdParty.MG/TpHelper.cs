using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.MG
{
    public class TpHelper: ThirdPartyHelperBase
    {
        private const string BasePath = "/api/v1";
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = GetInputValue(inputs, "ApiUrl");
            var tokenUrl = GetInputValue(inputs, "TokenUrl");
            var strAgents = GetInputValue(inputs, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCustInfo.TpCurrencyId);

            var parameters =
                $"client_id={agent.AgentCode}&client_secret={agent.SecretKey}&grant_type=client_credentials";
            var tokenResult = HttpPost($"{tokenUrl}/connect/token", parameters, "application/x-www-form-urlencoded");

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);

                var header = new Dictionary<string, string>()
                {
                    {"Authorization", $"Bearer {dict["access_token"]}"}
                };
                parameters = $"playerId={tpCustInfo.TpUserName}";
                var playerResult = HttpPost($"{apiUrl}{BasePath}/agents/{agent.AgentCode}/players", parameters, "application/x-www-form-urlencoded", header);

                return new ApiResult
                {
                    ErrorCode = ErrorCode.Success
                };
            }
            catch(WebException e)
            {
                var message = "";
                var code = "";
                using (WebResponse response = e.Response)
                {
                    var httpResponse = (HttpWebResponse) response;
                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        using (var data = response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            var text = reader.ReadToEnd();
                            var error = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                            message = error["message"];
                            code = error["code"];
                        }
                    }
                    else
                    {
                        message = e.Message;
                    }
                }
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = message,
                    RawErrorCode = code,
                    RawMessage = message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = GetInputValue(inputs, "ApiUrl");
            var tokenUrl = GetInputValue(inputs, "TokenUrl");
            var strAgents = GetInputValue(inputs, "Agent");
            var mgAgents = JsonConvert.DeserializeObject<List<MGAgents>>(strAgents);
            var agent = mgAgents.SingleOrDefault(a => a.Currency == tpCustInfo.TpCurrencyId);

            var parameters =
                $"client_id={agent.AgentCode}&client_secret={agent.SecretKey}&grant_type=client_credentials";
            var tokenResult = HttpPost($"{tokenUrl}/connect/token", parameters, "application/x-www-form-urlencoded");
            
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResult);

            var header = new Dictionary<string, string>()
            {
                {"Authorization", $"Bearer {dict["access_token"]}"}
            };
            parameters = $"contentType=Game&contentCode=SMG_10000Wishes&platform=Desktop&langCode=zh-CN";
            
            try
            {
                var sessionResult = HttpPost($"{apiUrl}{BasePath}/agents/{agent.AgentCode}/players/{tpCustInfo.TpUserName}/sessions", parameters, "application/x-www-form-urlencoded", header);
                var con = JsonConvert.DeserializeObject<Dictionary<string, string>>(sessionResult);
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{con["url"]}"
                };
            }
            catch (WebException e)
            {
                var message = "";
                var code = "";
                using (WebResponse response = e.Response)
                {
                    var httpResponse = (HttpWebResponse) response;
                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        using (var data = response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            var text = reader.ReadToEnd();
                            var error = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                            message = error["message"];
                            code = error["code"];
                        }
                    }
                    else
                    {
                        message = e.Message;
                    }
                }
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = message,
                    RawErrorCode = code,
                    RawMessage = message
                };
            }
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        private string GetInputValue(Dictionary<string, string> inputs, string key)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }
            throw new Exception();
        }
        
        private class MGAgents
        {
            public string Currency { get; set; }
            public string MasterAgent { get; set; }
            public string AgentCode { get; set; }
            public string SecretKey { get; set; }
        }
    }
}