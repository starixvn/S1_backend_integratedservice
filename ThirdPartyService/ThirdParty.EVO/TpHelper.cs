using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.EVO
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly EVOConfig _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<EVOConfig>(TpEnum.EVO.ToString());
        }

        #region Response

        public class LoginResponse
        {
            public string entry { get; set; }
            public string entryEmbedded { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}";
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new()
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string ApiUrl = _siteConfig.ApiUrl_Backup;
            string Ua2Token = _siteConfig.Ua2Token;
            string CasinoKey = _siteConfig.CasinoKey;

            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string GameUrl = ApiUrl;
            ApiUrl = $"{ApiUrl}/ua/v1/{CasinoKey}/{Ua2Token}";

            var tpId = (int)TpEnum.EVO;
            var tpCountryCode = TpCustInfoProvider.GetTpCurrencyCodeByCurrencyId(tpId, tpCustInfo.TpCurrencyId);
            string countryCode = !string.IsNullOrEmpty(tpCountryCode.CountryCode) ? tpCountryCode.CountryCode : throw new Exception("CountryCode");

            bool mobile = appType != AppType.Desktop;
            var siteName = ServerInfoProvider.GetSiteName(tpCustInfo.SiteId);

            ApiResult oApiResult = new();

            if (string.IsNullOrEmpty(siteName))
            {
                oApiResult.RawMessage = "Site Error";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var gameType = (int)TpGameType.EVO_LiveCasino;
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameType.ToString(), tpId.ToString() };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var Login = new
            {
                uuid = Guid.NewGuid().ToString(),
                player = new
                {
                    id = tpCustInfo.TpUserName,
                    update = true,
                    country = countryCode,
                    language = TpLangId,
                    currency = tpCustInfo.TpCurrencyId,
                    session = new
                    {
                        id = sessionKey,
                        ip = inputs["ClientIP"]
                    },
                },
                config = new
                {
                    brand = new
                    {
                        id = siteName.ToString(),
                        skin = "1"
                    },
                    game = new
                    {
                        category = "top_games",// baccarat
                        //table = new
                        //{
                        //    id = "k2oswnib7jjaaznw"
                        //},
                        //playMode = "reward_games"
                    },
                    channel = new
                    {
                        wrapped = mobile,
                        mobile = mobile
                    },
                    urls = new
                    {
                        lobby = homeUrl
                    },
                }
            };

            var result = HttpPost(
                 ApiUrl,
                 JsonConvert.SerializeObject(Login),
                 "application/json"
             );

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}{json.entry}";

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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_EVO_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            if (wagerDetail != null)
            {
                //general data
                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                        RefTransId = wagerDetail.GetValueOrDefault("transactionId")?.ToString(),
                        TpUserName = wagerDetail.GetValueOrDefault("playerId")?.ToString(),
                        UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("placedOn", DateTime.MinValue),
                        UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("settledAt", DateTime.MinValue),
                        Stake = (decimal)wagerDetail.GetValueOrDefault("stake") * tpCurrency.ExchangeRate,
                        WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("payout") - (decimal)wagerDetail.GetValueOrDefault("stake")) * tpCurrency.ExchangeRate,
                        GameCode = wagerDetail.GetValueOrDefault("gameType")?.ToString(),
                        GameName = wagerDetail.GetValueOrDefault("gameType")?.ToString(),
                    },
                    _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                    }
                };
            }
            else
            {
                //promo_payout data
                wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = getBetDetail.TransId }, DataBase.Report);

                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                        RefTransId = wagerDetail.GetValueOrDefault("promotionTransactionId")?.ToString(),
                        TpUserName = wagerDetail.GetValueOrDefault("userId")?.ToString(),
                        UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("CreateTime", DateTime.MinValue),
                        UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("UpdateTime", DateTime.MinValue),
                        Stake = 0,
                        WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("payout"),
                        GameCode = wagerDetail.GetValueOrDefault("gameId")?.ToString(),
                        GameName = wagerDetail.GetValueOrDefault("gameType")?.ToString()
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

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_EVO_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            if (detail != null)
            {
                string roundId = detail.GetStringValue("RoundId").Length <= 40 ? detail.GetStringValue("RoundId") : detail.GetStringValue("RoundId")[..40];
                return tpGameType.CategoryId switch
                {
                    TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                    {
                        GameID = detail.GetStringValue("gameType"),
                        Game = detail.GetStringValue("gameType"),
                        RefNo = detail.GetStringValue("transactionId"),
                        Title = detail.GetStringValue("description"),
                        MatchID = roundId,
                        BetType = string.Empty,
                        BetChoice = string.Empty,
                        Result = detail.GetStringValue("result"),
                        DeviceType = GetDeviceType(detail.GetStringValue("channel")),
                        IP = string.Empty,
                    },
                    _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                };
            }
            else
            {
                //promo_payout data
                detail = TpBetRecordProvider.DbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = betRecordInfo.TransId }, DataBase.Report);
                if (detail == null)
                {
                    return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
                }
                return tpGameType.CategoryId switch
                {
                    TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                    {
                        GameID = detail.GetStringValue("gameId"),
                        Game = detail.GetStringValue("gameType"),
                        RefNo = detail.GetStringValue("PromoTransactionRewardId"),
                        Title = detail.GetStringValue("PromoTransactionType"),
                        MatchID = detail.GetStringValue("TableId"),
                        BetType = string.Empty,
                        BetChoice = string.Empty,
                        Result = detail.GetStringValue("game"),
                        DeviceType = DeviceType.None,
                        IP = string.Empty,
                    },
                    _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                };
            }
        }

        private static DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "desktop" => DeviceType.Desktop,
                "mobile " => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }
    }
}
