using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.WM
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, WMConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WMConfig>(TpEnum.WM.ToString());
        }

        #region Request

        public class SignupReq
        {
            public string cmd { get; set; }
            public string vendorId { get; set; }
            public string signature { get; set; }
            public string user { get; set; }
            public string password { get; set; }
            public string username { get; set; }
            public long timestamp { get; set; }
            public int syslang { get; set; }
        }

        private class ApiResponse
        {
            public int errorCode { get; set; }
            public string errorMessage { get; set; }
            public string result { get; set; }
        }

        public class LoginReq
        {
            public string cmd { get; set; }
            public string vendorId { get; set; }
            public string signature { get; set; }
            public string user { get; set; }
            public string password { get; set; }
            public int lang { get; set; }
            //public int isTest { get; set; }
            public long timestamp { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var currencyData = config.CurrencyData[currency];

            string ApiUrl = config.ApiUrl;
            string vendorId = currencyData.vendorId;
            string signature = currencyData.signature;

            var SignUp = new
            {
                cmd = "MemberRegister",
                vendorId = vendorId,
                signature = HttpUtility.UrlEncode(signature),
                user = tpCustInfo.TpUserName,
                password = HttpUtility.UrlEncode(tpCustInfo.TpPwd),
                username = tpCustInfo.TpUserName,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                //syslang = 1,
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (json.errorCode == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else if (json.errorCode == 104)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.errorMessage;
                    oApiResult.RawErrorCode = json.errorCode.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

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
            string currency = tpCustInfo.TpCurrencyId;
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var currencyData = config.CurrencyData[currency];

            string ApiUrl = config.ApiUrl;
            string vendorId = currencyData.vendorId;
            string signature = currencyData.signature;
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var Login = new
            {
                cmd = "LoginGame",
                vendorId = vendorId,
                signature = HttpUtility.UrlEncode(signature),
                user = tpCustInfo.TpUserName,
                password = HttpUtility.UrlEncode(tpCustInfo.TpPwd),
                lang = TpLangId,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                returnurl = homeUrl
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Login));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (json.errorCode == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.result;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.errorMessage;
                    oApiResult.RawErrorCode = json.errorCode.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_WM_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("betId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("user")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("betTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("settime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("bet") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winLoss") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gname")?.ToString(),
                    GameName = wagerDetail.GetValueOrDefault("gname")?.ToString(),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_WM_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("gid"),
                    Game = detail.GetStringValue("gname"),
                    RefNo = detail.GetStringValue("betId"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("round"),
                    BetType = detail.GetStringValue("betCode"),
                    BetChoice = detail.GetStringValue("betResult"),
                    Result = detail.GetStringValue("gameResult"),
                    DeviceType = DeviceType.None,
                    IP = detail.GetStringValue("ip"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }
    }
}
