using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.NLC
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly NLCConfig _siteConfig;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.NLC).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<NLCConfig>(TpEnum.NLC.ToString());
        }

        #region Response

        public class LoginResponse
        {
            [JsonProperty("entry")]
            public string Entry { get; set; }

            [JsonProperty("entryEmbedded")]
            public string EntryEmbedded { get; set; }
        }

        public class DetailResponse
        {
            [JsonProperty("result")]
            public Result Result { get; set; }
        }

        public class Result
        {
            [JsonProperty("link")]
            public string Link { get; set; }
        }

        private class Table
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

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
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfig;
            string ApiUrl = config.ApiUrl_Backup;
            string GameHistoryApiToken = config.GameHistoryApiToken;
            string CasinoKey = config.CasinoKey;

            string word = "://";
            string[] cutHost = ApiUrl.Split(word);
            string BetDetailUrl = $"{cutHost[0]}{word}{CasinoKey}:{GameHistoryApiToken}@{cutHost[1]}";

            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_NLC_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            if (wagerDetail != null)
            {
                var detail = JsonConvert.DeserializeObject<Result>(wagerDetail.GetStringValue("result"));
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = $"{BetDetailUrl}{detail.Link}",
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            else
            {
                //Reward Data

                var tpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                };

                wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = getBetDetail.TransId }, DataBase.Report);

                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new CasinoDetail(getBetDetail.TpGameTypeOption)
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

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {

            var detailList = TpBetRecordProvider.DbQuery("Service_TpWagerRng_NLC_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            if (detailList != null)
            {
                var table = JsonConvert.DeserializeObject<Table>(detailList.GetFirstStringValue("table"));
                var gameCode = table.Id;
                var channel = detailList.GetFirstStringValue("channel");
                return tpGameType.CategoryId switch
                {
                    TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                    {
                        GameID = gameCode,
                        Game = GetGameName(gameCode),
                        RefNo = detailList.GetFirstStringValue("TransactionId"),
                        Title = tpGameType.TpName,
                        MatchID = detailList.GetFirstStringValue("GameId"),
                        BetType = string.Empty,
                        BetChoice = string.Empty,
                        Result = string.Empty,
                        DeviceType = GetDeviceType(channel),
                        IP = string.Empty,
                    },
                    _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                };
            }
            else
            {
                //promo_payout data
                var detail = TpBetRecordProvider.DbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = betRecordInfo.TransId }, DataBase.Report);
                if (detail == null)
                {
                    return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
                }
                return tpGameType.CategoryId switch
                {
                    TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfig;
            string ApiUrl = config.ApiUrl_Backup;
            string Ua2Token = config.Ua2Token;
            string CasinoKey = config.CasinoKey;

            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var LoginApiUrl = $"{ApiUrl}/ua/v1/{CasinoKey}/{Ua2Token}";

            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            bool mobile = appType != AppType.Desktop;

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                var demo = GetPlayDemo(LoginApiUrl, ApiUrl, TpLangId, gameId, mobile);
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = demo;
                return oApiResult;
            }

            var tpId = (int)TpEnum.NLC;
            var tpCountryCode = TpCustInfoProvider.GetTpCurrencyCodeByCurrencyId(tpId, tpCustInfo.TpCurrencyId);
            string countryCode = !string.IsNullOrEmpty(tpCountryCode.CountryCode) ? tpCountryCode.CountryCode : throw new Exception("CountryCode");

            var siteName = ServerInfoProvider.GetSiteName(tpCustInfo.SiteId);
            if (string.IsNullOrEmpty(siteName))
            {
                oApiResult.RawMessage = "Site Error";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var gameType = (int)TpGameType.NLC_RNG;
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
                        category = "slots",
                        table = new
                        {
                            id = gameId
                        },
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
                 LoginApiUrl,
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
            oApiResult.Message = $"{ApiUrl}{json.Entry}";

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        private string GetPlayDemo(string LoginApiUrl, string ApiUrl, string TpLangId, string GameId, bool Mobile)
        {
            var Login = new
            {
                uuid = Guid.NewGuid().ToString(),
                demo = new
                {
                    currency = "EUR",
                    language = TpLangId,
                },
                config = new
                {
                    game = new
                    {
                        category = "slots",
                        table = new
                        {
                            id = GameId
                        },
                        playMode = "demo",
                    },
                    channel = new
                    {
                        wrapped = Mobile,
                        mobile = Mobile
                    }
                }
            };

            var result = HttpPost(
                LoginApiUrl,
                JsonConvert.SerializeObject(Login),
                "application/json"
            );

            if (!IsJSON(result))
            {
                throw new Exception("GetPlayDemo Format Failed");
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            if (json.Entry == null)
            {
                throw new Exception("GetPlayDemo Entry Failed");
            }

            return $"{ApiUrl}{json.Entry}";
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        private DeviceType GetDeviceType(string channel)
        {
            DeviceType deviceType;

            switch (channel.ToLower())
            {
                case "desktop":
                    deviceType = DeviceType.Desktop;
                    break;
                case "mobile":
                    deviceType = DeviceType.Mobile;
                    break;
                default:
                    deviceType = DeviceType.None;
                    break;
            }

            return deviceType;

        }
    }
}