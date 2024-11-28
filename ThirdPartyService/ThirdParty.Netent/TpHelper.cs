using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Netent
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly NetentConfig _siteConfig;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Netent).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<NetentConfig>(TpEnum.Netent.ToString());
        }

        #region Response

        public class LoginResponse
        {
            public string entry { get; set; }
            public string entryEmbedded { get; set; }
        }

        public class DetailResponse
        {
            public Result result { get; set; }
        }

        public class Result
        {
            public string link { get; set; }
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

            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string GameUrl = ApiUrl;
            ApiUrl = $"{ApiUrl}/ua/v1/{CasinoKey}/{Ua2Token}";

            var tpId = (int)TpEnum.Netent;
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
            var gameType = (int)TpGameType.Netent_RNG;
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
                        //@interface = "view1",
                        table = new
                        {
                            id = gameId
                        },
                        //playMode = fun == "1" ? "play_for_fun" : "real_money"
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
            string ApiUrl = _siteConfig.ApiUrl_Backup;
            string GameProvider = _siteConfig.GameProvider;
            string GameHistoryApiToken = _siteConfig.GameHistoryApiToken;
            string CasinoKey = _siteConfig.CasinoKey;

            var WagerUrl = $"{ApiUrl}/api/gamehistory/v1/casino/games/stream";

            string word = "://";
            string[] cutHost = ApiUrl.Split(word);
            string BetDetailUrl = $"{cutHost[0]}{word}{CasinoKey}:{GameHistoryApiToken}@{cutHost[1]}";

            var slotData = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_Netent_Detail_Get", new { TransId = getBetDetail.TransId }, DataBase.Report);

            if (slotData != null)
            {
                //General Data
                var BetDetail = new
                {
                    startDate = slotData.GetValueOrDefault("CreateTime")?.ToString(),
                    gameProvider = GameProvider,
                    transactionId = getBetDetail.TransId
                };


                var dicP = FromJson<Dictionary<string, string>>(ToJson(BetDetail));
                var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                Dictionary<string, string> header = new()
                {
                    { "Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{CasinoKey}:{GameHistoryApiToken}")) }
                };

                var result = HttpGet(
                    WagerUrl,
                    sParam,
                    "text/plain",
                    header
                );

                if (!IsJSON(result))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result}");
                }

                var json = JsonConvert.DeserializeObject<DetailResponse>(result);
                if (json.result == null || string.IsNullOrEmpty(json.result.link))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail is null.");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = $"{BetDetailUrl}{json.result.link}",
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

                var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = getBetDetail.TransId }, DataBase.Report);

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

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Netent_Detail_GetAll", new { betRecordInfo.TransId }, DataBase.Main);
            if (detailList.Any())
            {
                var gameCode = detailList.GetFirstStringValue("GameDetailsId");
                return tpGameType.CategoryId switch
                {
                    TpCategory.Slots => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                    {
                        GameID = gameCode,
                        Game = GetGameName(gameCode),
                        RefNo = detailList.GetFirstStringValue("TransactionId"),
                        Title = tpGameType.TpName,
                        MatchID = detailList.GetFirstStringValue("GameId"),
                        BetType = string.Empty,
                        BetChoice = string.Empty,
                        Result = string.Empty,
                        DeviceType = DeviceType.None,
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
                    TpCategory.Slots => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
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

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }
    }
}
