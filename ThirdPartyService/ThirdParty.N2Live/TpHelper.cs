using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.N2Live
{
    public class TpHelper : ThirdPartyHelperBase
    {
        //[XmlRoot(ElementName = "n2root", Namespace = "urn:n2ns")]
        //public class N2root
        //{
        //    [XmlElement(ElementName = "request")]
        //    public Request Request { get; set; }
        //    [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
        //    public string N2xsd { get; set; }
        //}

        //[XmlRoot(ElementName = "request")]
        //public class Request
        //{
        //    [XmlElement(ElementName = "userid")]
        //    public string Userid { get; set; }
        //    [XmlElement(ElementName = "vendorid")]
        //    public string Vendorid { get; set; }
        //    [XmlElement(ElementName = "sessiontoken")]
        //    public string Sessiontoken { get; set; }
        //    [XmlElement(ElementName = "clientip")]
        //    public string Clientip { get; set; }
        //    [XmlElement(ElementName = "timestamp")]
        //    public string Timestamp { get; set; }
        //    [XmlAttribute(AttributeName = "action")]
        //    public string Action { get; set; }
        //    [XmlAttribute(AttributeName = "id")]
        //    public string Id { get; set; }
        //}

        public class LoginRequest
        {
            public LoginReq request { get; set; }
        }

        public class LoginReq
        {
            public string action { get; set; }
            public string id { get; set; }
            public string userid { get; set; }
            public int vendorid { get; set; }
            public string sessiontoken { get; set; }
            public string clientip { get; set; }
            public string timestamp { get; set; }

        }

        public class LoginResult
        {
            public string source { get; set; }
            public int status { get; set; }
            public LoginRes result { get; set; }

        }

        public class LoginRes
        {
            public string action { get; set; }
            public string id { get; set; }
            public string userid { get; set; }
            public string username { get; set; }
            public string acode { get; set; }
            public int currencyid { get; set; }
            public decimal balance { get; set; }
            public int vendorid { get; set; }
            public string merchantpasscode { get; set; }
            public string sessiontoken { get; set; }
            public string clientip { get; set; }

        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

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
            var SiteId = tpCustInfo.SiteId;
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception("GameUrl");
            string MobileUrl = inputs.ContainsKey("MobileUrl") && !string.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception("MobileUrl");
            string MerchantCode = inputs.ContainsKey($"MerchantCode_{SiteId}") && !string.IsNullOrEmpty(inputs[$"MerchantCode_{SiteId}"]) ? inputs[$"MerchantCode_{SiteId}"] : throw new Exception($"MerchantCode_{SiteId}");

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.SiteId.ToString(), tpCustInfo.TpCurrencyId };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            string LoginUrl = GameUrl;
            if (appType != AppType.Desktop)
            {
                LoginUrl = MobileUrl;
            }

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}&redirectURL=";

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
