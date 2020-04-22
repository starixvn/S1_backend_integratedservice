using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Text;

namespace ThirdParty.PP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class UserReq
        {
            public string username { get; set; }
            public string password { get; set; }
            public string providerId { get; set; }
        }
        public class gameReq
        {
            public string gameID { get; set; }
            public string gameName { get; set; }
            public string gameTypeID { get; set; }
            public string typeDescription { get; set; }
            public string technology { get; set; }
            public string platform { get; set; }
            public bool demoGameAvailable { get; set; }
            public string aspectRatio { get; set; }
            public string technologyID { get; set; }
            public int gameIdNumeric { get; set; }
            public List<string> jurisdictions { get; set; }
        }
        public class LoginReq
        {
            public string token { get; set; } //由玩家的运营商生成的安全令牌
            public string symbol { get; set; } //游戏的唯一标识符，如 vs20bl、vs15ad 等
            public string technology { get; set; } // H5 或 F（用于 WEB 平台）或 H5（用于 MOBILE 平台）
            public string platform { get; set; } //WEB（用于桌面设备）或 MOBILE（用于移动设备）
            public string language { get; set; } //ISO 639 - 1 标准的玩家语言（如 en、fr、it）
            public string cashierUrl { get; set; } //用于打开运营商网站收银台页面的 URL；
            public string lobbyUrl { get; set; } //用于打开运营商网站大厅页面的 URL；
            public string operatorGameHistoryUrl { get; set; }  //用于在运营商侧打开游戏历史记录页面的 URL
            //public string secureLogin { get; set; }  //运营商在 Pragmatic Play 侧的唯一标识符。
        }

        public class UserResult
        {
            public string userId { get; set; }
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }

            //"userId":"421", "currency":"USD", "cash": 99999.99, "bonus": 99.99, "error":0, "description":"Success"
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //throw new NotImplementedException();
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
            //throw new NotImplementedException();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string ProviderId = inputs.ContainsKey("ProviderId") && !string.IsNullOrEmpty(inputs["ProviderId"]) ? inputs["ProviderId"] : throw new Exception();
            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string SecureLogin = inputs.ContainsKey("SecureLogin") && !string.IsNullOrEmpty(inputs["SecureLogin"]) ? inputs["SecureLogin"] : throw new Exception();
            string SecurePassword = inputs.ContainsKey("SecurePassword") && !string.IsNullOrEmpty(inputs["SecurePassword"]) ? inputs["SecurePassword"] : throw new Exception();

            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";

            string sParam = "";
            LoginReq loginReq = new LoginReq();

            //gameId = "vs10bookoftut";
            loginReq.token = ApplyToken(tpCustInfo); 
            loginReq.symbol = gameId;
            loginReq.technology = "H5";
            //loginReq.technology
            loginReq.platform = (appType == AppType.Desktop) ? "WEB" : "MOBILE";
            loginReq.language = TpLangId;
            loginReq.lobbyUrl = lobbyUrl;
            loginReq.cashierUrl = cashierUrl;
            //loginReq.operatorGameHistoryUrl = "";

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            sParam = string.Join(System.Web.HttpUtility.UrlEncode("&"), dicP.Select(kp => $"{kp.Key}{System.Web.HttpUtility.UrlEncode("=")}{kp.Value}").ToArray());
            sParam = $"{sParam}&stylename={SecureLogin}";

            ApiResult oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{GameUrl}?key={sParam}"
            };
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
