using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Joker
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class UsrReq
        {
            public string AppID { get; set; }
            public string Hash { get; set; }
            public long Timestamp { get; set; }
        }
        public class LoginReq
        {
            public string token { get; set; }
            public string appID { get; set; }
            public string gameCode { get; set; }
            public string language { get; set; }
            public bool mobile { get; set; }
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
            //throw new NotImplementedException();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string AppID = inputs.ContainsKey($"AppID_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"AppID_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"AppID_{tpCustInfo.TpCurrencyId}"] : throw new Exception();
            string Secret = inputs.ContainsKey($"Secret_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"Secret_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"Secret_{tpCustInfo.TpCurrencyId}"] : throw new Exception();

            if (tpCustInfo.IsTest)
            {
                AppID = inputs.ContainsKey($"AppID_{tpCustInfo.TpCurrencyId}_Test") && !string.IsNullOrEmpty(inputs[$"AppID_{tpCustInfo.TpCurrencyId}_Test"]) ? inputs[$"AppID_{tpCustInfo.TpCurrencyId}_Test"] : throw new Exception();
                Secret = inputs.ContainsKey($"Secret_{tpCustInfo.TpCurrencyId}_Test") && !string.IsNullOrEmpty(inputs[$"Secret_{tpCustInfo.TpCurrencyId}_Test"]) ? inputs[$"Secret_{tpCustInfo.TpCurrencyId}_Test"] : throw new Exception();
            }

            ApiUrl = $"{ApiUrl}";

            LoginReq loginReq = new LoginReq();
            loginReq.token = ApplyToken(tpCustInfo);
            loginReq.appID = AppID;
            loginReq.gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            loginReq.language = TpLangId;
            loginReq.mobile = appType != AppType.Desktop;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());
            //var result = HttpPost(ApiUrl, sParam, "application/json");
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?{sParam}";
            return oApiResult;
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
