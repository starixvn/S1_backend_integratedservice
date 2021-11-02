using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Xml;

namespace ThirdParty.AG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        Dictionary<string, string> oddsTypeDic = new Dictionary<string, string>() {
            {"MYR","GF4B" },
            {"CNY","C" },
            {"THB","GF4A" },
            {"IDR","GF4A" },
            {"VND ","GF4A" },
            {"USD ","GF4A" },
            {"INR ","GF4A" }
        };

        public class result
        {
            public string info { get; set; }
            public string msg { get; set; }
        }

        public class aa
        {
            public string ResponseCode { get; set; }
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string actype = tpCustInfo.IsTest ? "0" : "1";
            string cagent = inputs.ContainsKey("cagent") && !string.IsNullOrEmpty(inputs["cagent"]) ? inputs["cagent"] : throw new Exception();
            string apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception();
            
            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/method=lg/\\\\/actype={actype}/\\\\/password={tpCustInfo.TpPwd}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}";
            string eParams = DESEncrypt(sParams, inputs["Deskey"]);
            string oMD5 = $"{eParams}{Md5key}";
            string postParams = $"params={eParams}&key={ ToMD5(oMD5).ToLower() }";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(apiUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();

            if (IsXML(sResponse))
            {
                var oCreateMemberResp = FromXml<result>(sResponse);
                switch (oCreateMemberResp.info.ToString())
                {
                    case "0":
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = oCreateMemberResp.msg;
                        break;
                    case "key_error":
                        oApiResult.ErrorCode = ErrorCode.KEY_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "network_error":
                        oApiResult.ErrorCode = ErrorCode.NETWORK_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "account_add_fail":
                        oApiResult.ErrorCode = ErrorCode.Fail;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "error":
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    default:
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = sResponse;
            }

            return oApiResult;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.ThirdId = $"";
            transIdModel.Implemented = false;

            return transIdModel;

        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string actype = tpCustInfo.IsTest ? "0" : "1";
            string cagent = inputs.ContainsKey("cagent") && !string.IsNullOrEmpty(inputs["cagent"]) ? inputs["cagent"] : throw new Exception();
            string data_cagent = inputs.ContainsKey("data_cagent") && !string.IsNullOrEmpty(inputs["data_cagent"]) ? inputs["data_cagent"] : throw new Exception();
            string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception();
            string SessionUrl = inputs.ContainsKey("SessionUrl") && !string.IsNullOrEmpty(inputs["SessionUrl"]) ? inputs["SessionUrl"] : throw new Exception();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();

            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            //string oddtype = "";
            string balance = "";
            string token = "";
            inputs.TryGetValue("Balance", out balance);
            inputs.TryGetValue("Token", out token);
            string postParams = $"productid={data_cagent}&username={tpCustInfo.TpUserName}&session_token={token}&credit={balance}";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(SessionUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();
            {
                if (IsXML(sResponse))
                {
                    var ResponseCode = FromXmlNode(sResponse);
                    switch (ResponseCode.ToString())
                    {
                        case "OK":
                            break;
                        default:
                            oApiResult.ErrorCode = ErrorCode.ServerResponseError;
                            oApiResult.RawMessage = ResponseCode;
                            return oApiResult;
                    }
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = sResponse;
                    return oApiResult;
                }
            }


            long sTimestemp = GetTimestemp1();

            string dm = "www.nova88.net";
            if (inputs.ContainsKey("host"))
            {
                dm = inputs["host"];
            }

            var useLangId = (TpLangId == "zh-cn") ? 1: 3;
            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/password={tpCustInfo.TpPwd}/\\\\/actype={actype}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}/\\\\/dm={dm}/\\\\/sid={cagent}{sTimestemp}/\\\\/lang={useLangId}";

            if (inputs.ContainsKey("gameType") && !string.IsNullOrEmpty(inputs["gameType"]))
            {
                sParams = $"{sParams}/\\\\/gameType={inputs["gameType"]}";
            }

            string eParams = DESEncrypt(sParams, inputs["Deskey"]);
            string oMD5 = $"{eParams}{Md5key}";
            postParams = $"params={eParams}&key={ ToMD5(oMD5).ToLower() }";

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?{postParams}";
            return oApiResult;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        protected T FromXml<T>(string xml) where T : new()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlElement root = doc.DocumentElement;

            XmlAttributeCollection nodeList = root.Attributes;

            var data = new T();

            foreach (var key in data.GetType().GetProperties())
            {
                key.SetValue(data, nodeList.GetNamedItem(key.Name).Value);

            }
            return data;
        }

        protected string FromXmlNode(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement root = doc.DocumentElement;
            XmlAttributeCollection nodeList = root.Attributes;
            XmlNodeList nodes = doc.DocumentElement.SelectNodes("/PlayerTicketResponse");
            return nodes[0].SelectSingleNode("ResponseCode").InnerText; ;
        }
    }
}
