

using ThirdPartyCore;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.TpBetRecordExtend;
using System.Text;
using System.Web;
using ThirdPartyCore.Extensions;

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
            string RetrieveUrl = inputs.ContainsKey($"RetrieveUrl_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"RetrieveUrl_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"RetrieveUrl_{tpCustInfo.TpCurrencyId}"] : throw new Exception();
            string PublicToken = inputs.ContainsKey($"PublicToken_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"PublicToken_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"PublicToken_{tpCustInfo.TpCurrencyId}"] : throw new Exception();

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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string PrivateToken = inputs.ContainsKey($"PrivateToken_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"PrivateToken_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"PrivateToken_{tpCustInfo.TpCurrencyId}"] : throw new Exception();

            string url = $"https://{ApiUrl}/api/v2/bet-transaction/";
            string getParams = $"id={getBetDetail.TransId}";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Authorization", PrivateToken);
            string json = HttpGet(url, getParams, "application/json", header);
            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = json,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerTFG_Get", new { id = betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("event_id"),
                Game = tpGameType.TypeName,
                RefNo = string.Empty,
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = detail.GetStringValue("member_odds_style"),
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(detail.GetStringValue("request_source")),
                IP = string.Empty,
            };
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
        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "desktop-browser"=> DeviceType.Desktop,
                "mobile-browser"=> DeviceType.Mobile,
                "mobile-app"=> DeviceType.Mobile,
                _=> DeviceType.None
            };
        }
    }
}