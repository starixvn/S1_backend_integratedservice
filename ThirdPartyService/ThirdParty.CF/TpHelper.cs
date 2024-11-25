using System.Xml;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.CF
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class LoginReq
        {
            public string api_key { get; set; }
            public string agent_code { get; set; }
            public string login_id { get; set; }
            public string name { get; set; }
        }

        public class LoginResult
        {
            public GetSessionId get_session_id { set; get; }
        }

        public class GetSessionId
        {
            public string status_code { set; get; }

            public string status_text { set; get; }

            public string session_id { set; get; }

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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl is empty");
            string ApiKey = inputs.ContainsKey("api_key") && !string.IsNullOrEmpty(inputs["api_key"]) ? inputs["api_key"] : throw new Exception("api_key is empty");
            string AgentCode = inputs.ContainsKey("agent_code") && !string.IsNullOrEmpty(inputs["agent_code"]) ? inputs["agent_code"] : throw new Exception("agent_code is empty");
            string WebUrl = inputs.ContainsKey("WebUrl") && !string.IsNullOrEmpty(inputs["WebUrl"]) ? inputs["WebUrl"] : throw new Exception("WebUrl is empty");
            string MobileUrl = inputs.ContainsKey("MobileUrl") && !string.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception("MobileUrl is empty");

            LoginReq loginReq = new LoginReq
            {
                api_key = ApiKey,
                agent_code = AgentCode,
                login_id = tpCustInfo.TpUserName,
                name = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost($"{ApiUrl}/get_session_id.aspx", sParam, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsXML(result))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                var json = JsonConvert.SerializeXmlNode(xmlDoc);
                var xml = JsonConvert.DeserializeObject<LoginResult>(json);
                var tpData = xml.get_session_id;
                if (tpData.status_code == "00")
                {
                    var url = $"{WebUrl}/api/auth_login.aspx";
                    if (appType != AppType.Desktop)
                    {
                        url = $"{MobileUrl}/api/cash/auth";
                    }

                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{url}?session_id={tpData.session_id}&lang={TpLangId}&login_id={tpCustInfo.TpUserName}";
                }
                else
                {
                    oApiResult.RawMessage = tpData.status_text;
                    oApiResult.RawErrorCode = tpData.status_code;
                    oApiResult.Message = $"{tpData.status_code}-{tpData.status_text}";
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
