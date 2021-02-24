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
            string GameUrl = inputs.ContainsKey($"GameUrl_{SiteId}") && !string.IsNullOrEmpty(inputs[$"GameUrl_{SiteId}"]) ? inputs[$"GameUrl_{SiteId}"] : throw new Exception($"GameUrl_{SiteId}");
            string MobileUrl = inputs.ContainsKey($"MobileUrl_{SiteId}") && !string.IsNullOrEmpty(inputs[$"MobileUrl_{SiteId}"]) ? inputs[$"MobileUrl_{SiteId}"] : throw new Exception($"MobileUrl_{SiteId}");
            string MerchantCode = inputs.ContainsKey($"MerchantCode_{SiteId}") && !string.IsNullOrEmpty(inputs[$"MerchantCode_{SiteId}"]) ? inputs[$"MerchantCode_{SiteId}"] : throw new Exception($"MerchantCode_{SiteId}");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "homeUrl";

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
            oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}&redirectURL={homeUrl}";

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
