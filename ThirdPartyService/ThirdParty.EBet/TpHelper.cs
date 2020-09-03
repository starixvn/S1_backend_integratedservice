using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.EBet
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string GameUrl = inputs.ContainsKey($"GameUrl_{currency}") && !string.IsNullOrEmpty(inputs[$"GameUrl_{currency}"]) ? inputs[$"GameUrl_{currency}"] : throw new Exception($"GameUrl_{currency} is empty");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            ApiResult oApiResult = new ApiResult();

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            string funParam = "";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                funParam = "&mode=trial";
            }
            string mobileParam = "";
            if (appType != AppType.Desktop)
            {
                mobileParam = "&orientation=1";
            }

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?username={tpCustInfo.TpUserName}&accessToken={HttpUtility.UrlEncode(sessionKey)}&language={TpLangId}{mobileParam}{funParam}";
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}
