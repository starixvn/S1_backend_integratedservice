using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.WM
{
    public class TpHelper : ThirdPartyHelperBase
    {
        //private string userPassword = "VyeYHngmE794AXLrDPTN";

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


        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string vendorId = inputs.ContainsKey($"vendorId_{currency}") && !string.IsNullOrEmpty(inputs[$"vendorId_{currency}"]) ? inputs[$"vendorId_{currency}"] : throw new Exception($"vendorId_{currency}");
            string signature = inputs.ContainsKey($"signature_{currency}") && !string.IsNullOrEmpty(inputs[$"signature_{currency}"]) ? inputs[$"signature_{currency}"] : throw new Exception($"signature_{currency}");

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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string vendorId = inputs.ContainsKey($"vendorId_{currency}") && !string.IsNullOrEmpty(inputs[$"vendorId_{currency}"]) ? inputs[$"vendorId_{currency}"] : throw new Exception($"vendorId_{currency}");
            string signature = inputs.ContainsKey($"signature_{currency}") && !string.IsNullOrEmpty(inputs[$"signature_{currency}"]) ? inputs[$"signature_{currency}"] : throw new Exception("signature");
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
            var wagerDetail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_WM_Detail_Get", new { getBetDetail.TransId });
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
            var detail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_WM_Detail_Get", new { betRecordInfo.TransId });
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("gid"),
                    Game = detail.GetStringValue("gname"),
                    RefNo = detail.GetStringValue("betId"),
                    Title = string.Empty,
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
