using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.YGG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = new YggConfig(inputs);
            var channel = (appType == AppType.Desktop ? "pc" : "mobile");
            var sessionKey = GetSessionKey(tpCustInfo, inputs);

            // YGG 試玩遊戲，Fun = 1 是試玩 ，不用打Api，自組登入 Url。
            if (config.Fun.Equals("1", StringComparison.OrdinalIgnoreCase)) return GetPlayForFunLaunchUrl(TpLangId, config, channel);

            // YGG 正式遊戲

            var apiUrl = config.GameUrl;
            var sParam = $"loaginame={tpCustInfo.TpUserName}&key={sessionKey}&currency={tpCustInfo.TpCurrencyId}" +
                $"&lang={TpLangId}&gameid={config.GameId}&org={config.Org}&channel={channel}&home={config.HomeUrl}&fullscreen=yes";

            var responseJson = HttpPost(apiUrl, sParam, config.ContentType);
            var responseResult = FromJson<LoginResponse>(responseJson);

            if (responseResult.Code != 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = responseResult.ErrorMsg,
                    RawErrorCode = responseResult.Code.ToString(),
                    RawMessage = responseResult.ErrorMsg
                };
            }

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = responseResult.Data.LaunchUrl
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var detailList = TpBetRecordProvider.ReportDbQuery("Service_BettransRng_YGG_Detail_Get", new { getBetDetail.TransId })
                .Where(i => (sbyte)i.GetValueOrDefault("type") != 2);
            var detail = ToJson(detailList);

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = string.Empty,
                    TpUserName = detailList.FirstOrDefault()?.GetValueOrDefault("playerid")?.ToString(),
                    UTCBetTime = detailList.Min(i => (DateTime)i.GetValueOrDefault("CreateTime", DateTime.MinValue)),
                    UTCSettledTime = detailList.Max(i => (DateTime)i.GetValueOrDefault("CreateTime", DateTime.MinValue)),
                    Stake = detailList.Where(i => (sbyte)i.GetValueOrDefault("type") == 1)
                        .Sum(i => Math.Abs((decimal)i.GetValueOrDefault("amount"))) * tpCurrency.ExchangeRate,
                    WinlostAmount = detailList.Sum(i => (decimal)i.GetValueOrDefault("amount")) * tpCurrency.ExchangeRate,
                    GameCode = detailList.FirstOrDefault()?.GetValueOrDefault("cat3")?.ToString(),
                    GameName = detailList.FirstOrDefault()?.GetValueOrDefault("cat4")?.ToString(),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        #region 不實作

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region config & privateMethod

        private class YggConfig
        {
            /// <summary>
            /// login Api Url
            /// </summary>
            public string GameUrl { get; }

            /// <summary>
            /// 試玩Url
            /// </summary>
            public string FunGameUrl { get; }

            /// <summary>
            /// 品牌Id
            /// </summary>
            public string Org { get; }

            /// <summary>
            /// 試玩 0 否 1是
            /// </summary>
            public string Fun { get; }

            /// <summary>
            /// 子遊戲Id
            /// </summary>
            public string GameId { get; }

            /// <summary>
            /// 我方首頁Url
            /// </summary>
            public string HomeUrl { get; }

            public string ContentType => "application/x-www-form-urlencoded";

            public YggConfig(Dictionary<string, string> inputs)
            {
                GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"])
                    ? inputs["GameUrl"]
                    : throw new Exception();

                FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"])
                    ? inputs["FunGameUrl"]
                    : throw new Exception();

                Org = inputs.ContainsKey("org") && !string.IsNullOrEmpty(inputs["org"])
                    ? inputs["org"]
                    : string.Empty;

                GameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"])
                    ? inputs["gameId"]
                    : string.Empty;

                HomeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"])
                    ? inputs["homeUrl"]
                    : string.Empty;

                Fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"])
                    ? inputs["fun"]
                    : "0";
            }
        }

        /// <summary>
        /// 取得 SessionKey
        /// </summary>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        private string GetSessionKey(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out var token);
            var sessionKeyArr = new string[] { key, token };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);
            return sessionKey;
        }

        /// <summary>
        /// 取得 YGG 試玩遊戲登入url
        /// </summary>
        /// <param name="TpLangId"></param>
        /// <param name="config"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private static ApiResult GetPlayForFunLaunchUrl(string TpLangId, YggConfig config, string channel)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message =
                    $"{config.FunGameUrl}?lang={TpLangId}&gameid={config.GameId}&org=demo&channel={channel}&home={HttpUtility.UrlEncode(config.HomeUrl)}&fullscreen=yes"
            };
        }

        public class LoginData
        {
            [JsonProperty("launchurl")]
            public string LaunchUrl { get; set; }
        }

        public class LoginResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("msg")]
            public string ErrorMsg { get; set; }

            [JsonProperty("data")]
            public LoginData Data { get; set; }
        }

        #endregion
    }
}
