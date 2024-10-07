using System.ComponentModel;
using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.YGG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, YGGConfig> _siteConfigDic;
        public readonly string _contentType = "application/x-www-form-urlencoded";

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YGGConfig>(TpEnum.YGG.ToString());
        }

        #region Response

        public class LoginData
        {
            [JsonProperty("launchurl")]
            public string LaunchUrl { get; set; }

            [Description("試玩")]
            [JsonProperty("pfflaunchurl")]
            public string pfflaunchurl { get; set; }
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
            var config = _siteConfigDic[tpCustInfo.SiteId];
            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            var GameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : string.Empty;
            var HomeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : string.Empty;
            var channel = appType == AppType.Desktop ? "pc" : "mobile";
            var sessionKey = GetSessionKey(tpCustInfo, inputs);

            var apiUrl = config.GameUrl;
            string sParam;
            var isFun = false;

            // 試玩
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                isFun = true;
                var DefaultTpCurrencyId = ServerInfoProvider.GetDefaultTpCurrencyIdByTpId(tpCustInfo.SiteId, (int)TpEnum.YGG);
                tpCustInfo.TpCurrencyId = (DefaultTpCurrencyId != null) ? DefaultTpCurrencyId.TpCurrencyId : "";
                apiUrl = config.FunGameUrl;
                sParam = $"org={config.org}&currency={tpCustInfo.TpCurrencyId}&lang={TpLangId}&gameid={GameId}&channel={channel}&home={HomeUrl}&fullscreen=yes";
            }
            else
            {
                // 正式
                sParam = $"loginname={tpCustInfo.TpUserName}&key={HttpUtility.UrlEncode(sessionKey)}&currency={tpCustInfo.TpCurrencyId}" +
               $"&lang={TpLangId}&gameid={GameId}&org={config.org}&channel={channel}&home={HomeUrl}&fullscreen=yes";
            }

            var responseJson = HttpPost(apiUrl, sParam, _contentType);
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
                Message = isFun ? responseResult.Data.pfflaunchurl : responseResult.Data.LaunchUrl
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
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_YGG_Detail_Get", new { getBetDetail.TransId }, DataBase.Report)
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

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_YGG_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detailList.GetFirstStringValue("cat5"),
                    Game = detailList.GetFirstStringValue("cat4"),
                    RefNo = detailList.GetFirstStringValue("reference"),
                    Title = detailList.GetFirstStringValue("tag1"),
                    MatchID = detailList.GetFirstStringValue("reference"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(detailList.GetFirstStringValue("tag3")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
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

        #region PrivateMethod

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
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            return sessionKey;
        }

        private static DeviceType GetDeviceType(string device)
        {
            if (device.Equals("Channel.PC", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Desktop;
            }

            if (device.Equals("Channel.Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.Mobile;
            }

            return DeviceType.None;
        }

        #endregion
    }
}
