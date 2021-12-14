using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.AP
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
            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion
            string WebUrl = inputs.ContainsKey("WebUrl") && !string.IsNullOrEmpty(inputs["WebUrl"]) ? inputs["WebUrl"] : throw new Exception();
            string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();


            string LangId = inputs.ContainsKey("LangId") && !string.IsNullOrEmpty(inputs["LangId"]) ? inputs["LangId"] : throw new Exception();
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            
            string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&LangCode={LangId}";

            string sResponse = HttpGet($"{WebUrl}LaunchGame", Params, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //#region token
            //var key = ApplyToken(tpCustInfo);
            //inputs.TryGetValue("Token", out string token);
            //var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            //var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            //var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            //#endregion

            //string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();
            //string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            //string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&GameRoundId={getBetDetail.TransId}";
            //string SecretKey = inputs.ContainsKey("SecretKey") && !string.IsNullOrEmpty(inputs["SecretKey"]) ? inputs["SecretKey"] : throw new Exception();
            //string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            //Dictionary<string, string> header = new Dictionary<string, string>();
            //header["ap-hash"] = ToMD5(SecretKey + Params);

            //string sResponse = HttpGet(GameUrl, Params, "application/x-www-form-urlencoded", header);
            //BaseDetail oApiResult = new BaseDetail(getBetDetail.TpGameTypeOption);
            //return oApiResult;
            throw new NotImplementedException();
        }
    }
}
