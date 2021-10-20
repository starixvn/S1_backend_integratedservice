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
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

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
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception("GameUrl");
            string MobileUrl = inputs.ContainsKey("MobileUrl") && !string.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception("MobileUrl");
            string MerchantCode = inputs.ContainsKey("MerchantCode") && !string.IsNullOrEmpty(inputs["MerchantCode"]) ? inputs["MerchantCode"] : throw new Exception("MerchantCode");
            //string errorUrl = inputs.ContainsKey("errorUrl") && !string.IsNullOrEmpty(inputs["errorUrl"]) ? inputs["errorUrl"] : "errorUrl";

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
            //oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}&redirectURL={errorUrl}";
            oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}";
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
            var wagerDetail = TpBetRecordProvider.GetWagerDetail("Service_TpWager_N2Live_Detail_Get", new { getBetDetail.TransId });
            var detail = ToJson(wagerDetail);

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("betid")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("login")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("startdate", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("enddate", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("bet_amount") * tpCurrency.ExchangeRate,
                    WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("payout_amount") - (decimal)wagerDetail.GetValueOrDefault("bet_amount")) * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gamecode")?.ToString(),
                    GameName = string.Empty,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

    }
}
