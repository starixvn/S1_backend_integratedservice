using Newtonsoft.Json;
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
        public class GetPlayerTokenrep
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string Token { get; set; }
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
            //string key = "3Lqoby7lVU68DbJJdMoAUQ";
            //string token = "0vckrjNh0OYKTRYzePRNw";
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion
            string WebUrl = inputs.ContainsKey("WebUrl") && !string.IsNullOrEmpty(inputs["WebUrl"]) ? inputs["WebUrl"] : throw new Exception();
            string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();


            string LangId = TpLangId;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            
            string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&LangCode={LangId}";

            //string sResponse = HttpGet($"{WebUrl}LaunchGame", Params, "application/x-www-form-urlencoded");
            string URL = $"{WebUrl}LaunchGame?{Params}";
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = URL;
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            #region token
            //var key = ApplyToken(tpCustInfo);
            //inputs.TryGetValue("Token", out string token);
            //var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            //var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            //var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            

            #endregion
            string WebUrl = inputs.ContainsKey("WebUrl") && !string.IsNullOrEmpty(inputs["WebUrl"]) ? inputs["WebUrl"] : throw new Exception();
            string LangId = inputs.ContainsKey("LangId") && !string.IsNullOrEmpty(inputs["LangId"]) ? inputs["LangId"] : throw new Exception();
            string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();
            string GameRoundId = getBetDetail.TransId;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            string PlayerToken = string.Empty;
            #region GetPlayerToken
            string uuid = Guid.NewGuid().ToString();
            string Timestamp = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            string sParam = $"Timestamp={Timestamp}&Seq={uuid}&SiteCode={SiteCode}&PlayerId={tpCustInfo.TpUserName}";
            var result = HttpPost($"{WebUrl}/GetPlayerToken", sParam, "application/x-www-form-urlencoded");

            if (IsJSON(result))
            {
                var response = JsonConvert.DeserializeObject<GetPlayerTokenrep>(result);
                PlayerToken = response.Token;
            }
            else
            {
                throw new NotImplementedException();
            }
            #endregion

            string Params = $"Token={PlayerToken}&SiteCode={SiteCode}&GameCode={gameId}&GameRoundId={GameRoundId}&LangCode={LangId}";

            string requrl = HttpGet($"{WebUrl}GameDetail", Params, "application/x-www-form-urlencoded");

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = requrl,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }
    }
}
