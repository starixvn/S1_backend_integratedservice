

using ThirdPartyCore;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.TpBetRecordExtend;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Web;

namespace ThirdParty.TFG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public TpHelper()
        {
        }
        public class GameURL
        {
            public string launch_url { get; set; }
        }
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string RetrieveUrl = inputs.ContainsKey("RetrieveUrl_VND") && !string.IsNullOrEmpty(inputs["RetrieveUrl_VND"]) ? inputs["RetrieveUrl_VND"] : throw new Exception();
            string PublicToken = inputs.ContainsKey("PublicToken_VND") && !string.IsNullOrEmpty(inputs["PublicToken_VND"]) ? inputs["PublicToken_VND"] : throw new Exception();
            string PartnerId = inputs.ContainsKey("PartnerId_VND") && !string.IsNullOrEmpty(inputs["PartnerId_VND"]) ? inputs["PartnerId_VND"] : throw new Exception();
            string PrivateToken = inputs.ContainsKey("PrivateToken_VND") && !string.IsNullOrEmpty(inputs["PrivateToken_VND"]) ? inputs["PrivateToken_VND"] : throw new Exception();

            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            string gameUrl = $"{RetrieveUrl}/api/v4/launch/url/";
            var sParam = $"auth={PublicToken}";
            string json = HttpGet(gameUrl, sParam, "application/json");
            ApiResult oApiResult = new ApiResult();
            {
                if (IsJSON(json))
                {
                    var result = FromJson<GameURL>(json);
                    if (!string.IsNullOrWhiteSpace(result.launch_url))
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = $"{result.launch_url}?auth={PublicToken}&token={sessionKey}";
                    }
                    else
                    {
                        oApiResult.RawMessage = "No Result";
                        oApiResult.RawErrorCode = "1";
                        oApiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
            }
            return oApiResult;
        }
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}