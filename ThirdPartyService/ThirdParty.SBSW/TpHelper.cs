using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SBSW
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyBetLimitParticular, IThirdPartyTokenParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";
        private readonly Dictionary<int, SabaConfig> siteSabaConfigs;

        public TpHelper(IServiceProvider provider)
        {
            siteSabaConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SabaConfig>(TpEnum.SBSW.ToString());
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var ApiUrl = $"{config.ApiUrl}/CreateMember";
            var input = new CreateMemberReq
            {
                vendor_id = config.VendorId,
                vendor_member_id = tpCustInfo.TpUserName,
                operatorId = config.OperatorId,
                userName = tpCustInfo.TpUserName,
                maxtransfer = int.MaxValue,
                mintransfer = 1,
                currency = tpCustInfo.IsTest ? 20 : Convert.ToInt32(tpCustInfo.TpCurrencyId),
                oddsType = "1",
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(input));
            var postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}"))}";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded", timeOutTime: 3);

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<Result>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var platform = (int)appType;
            if (inputs.TryGetValue("device", out var device) && device.Equals("wap", StringComparison.OrdinalIgnoreCase))
            {
                platform = 3;
            }

            // 取得進入網址         
            var loginResult = GetSabaUrl(config, tpCustInfo.TpUserName, platform);
            if (loginResult.error_code != 0)
            {
                return new ApiResult()
                {
                    RawMessage = loginResult.message,
                    RawErrorCode = loginResult.error_code.ToString(),
                    ErrorCode = ErrorCode.Fail
                };
            }

            // 檢查matchid
            GameDetail gameDetail = null;
            if (inputs.TryGetValue("matchid", out var matchid) && !string.IsNullOrEmpty(matchid))
            {
                var getGameDetailResult = GetGameDetail(config, matchid);
                if (getGameDetailResult.error_code == 0)
                {
                    gameDetail = getGameDetailResult.Data.FirstOrDefault();
                }
            }
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = new SBSWDeepLinkHelper(loginResult.Data, tpCustInfo, appType, TpLangId, inputs, gameDetail, config).GameUrl
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var input = new Req
            {
                vendor_id = config.VendorId,
                vendor_member_id = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            var ApiUrl = $"{config.ApiUrl}/KickUser";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded", timeOutTime: 3);

            ApiResult apiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<Result>(result);
                if (json.error_code == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
            }

            return apiResult;
        }

        public ApiResult<TransferResult> BonusTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal bonus, decimal lockedAmount, decimal rolloverAmount, long expiredDate, string transID, string thirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString, string tpCurrencyId)
        {
            throw new NotImplementedException();
        }

        public ApiResult SetBetLimit(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, string betSetting)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var req = new SetMemberBetSettingReq
            {
                vendor_id = config.VendorId,
                vendor_member_id = tpCustInfo.TpUserName,
                bet_setting = betSetting
            };
            var t = req.GetType();
            var props = t.GetProperties();
            var postData = $"{string.Join("&", props.Select(kp => $"{kp.Name}={kp.GetValue(req)}"))}";

            var apiUrl = $"{config.ApiUrl}/SetMemberBetSetting";
            var result = HttpPost(apiUrl, postData, "application/x-www-form-urlencoded", timeOutTime: 3);

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SetMemberBetSettingResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        private LoginResult GetSabaUrl(SabaConfig config, string tpUserName, int platform)
        {
            var apiUrl = $"{config.ApiUrl}/GetSabaUrl";
            var sParam = $"vendor_id={config.VendorId}&vendor_member_id={tpUserName}&platform={platform}";
            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 3);
            var result = FromJson<LoginResult>(json);
            return result;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var gameType = getBetDetail.TpGameTypeOption.Type;
            var tpBetRecordDetail = TpBetRecordProvider.GetTpBetRecordDetail(getBetDetail.TpGameTypeOption.TpId, gameType, getBetDetail.TransId, getBetDetail.SettledTime);

            if (tpBetRecordDetail == null)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }
            var detail = ChangeTransIdToString(tpBetRecordDetail.Detail);
            var model = JsonConvert.DeserializeObject<DetailModel>(tpBetRecordDetail.Detail);
            var betDetail = model?.BetDetails?.First();
            var betNumberDetail = model?.BetNumberDetails?.First();
            var betVirtualSportDetail = model?.BetVirtualSportDetails?.First();

            if (betDetail == null && betNumberDetail == null && betVirtualSportDetail == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                };
            }

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    TpUserName = betDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betDetail?.settlement_time, DateTime.MinValue),
                    Odds = betDetail?.odds,
                    Stake = betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betDetail.winlost_amount * tpCurrency.ExchangeRate,
                    LeagueName = betDetail?.leaguename?.First()?.name,
                    HomeTeamName = betDetail?.hometeamname?.First()?.name,
                    AwayTeamName = betDetail?.awayteamname?.First()?.name,
                    HomeHandicap = betDetail?.home_hdp,
                    AwayHandicap = betDetail?.away_hdp,
                    Handicap = betDetail?.hdp,
                    TicketStatus = betDetail?.ticket_status,
                    AwayScore = betDetail?.away_score,
                    HomeyScore = betDetail?.home_score,
                    RefTransId = betDetail?.parlay_ref_no,
                },
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = betDetail?.trans_id,
                    TpUserName = betDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betDetail?.settlement_time, (DateTime)betDetail?.transaction_time),
                    Stake = betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betDetail.winlost_amount * tpCurrency.ExchangeRate,
                    GameCode = string.Empty,
                    GameName = string.Empty,
                },
                TpCategory.Lottery => new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = gameType == (int)TpGameType.SBSW_NumberGame ? betNumberDetail?.trans_id : betDetail?.trans_id,
                    TpUserName = gameType == (int)TpGameType.SBSW_NumberGame ? betNumberDetail?.vendor_member_id : betDetail?.vendor_member_id,
                    UTCBetTime = gameType == (int)TpGameType.SBSW_NumberGame ? GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) :
                            GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = gameType == (int)TpGameType.SBSW_NumberGame ? GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) :
                            GetUTCTime(betDetail?.transaction_time, (DateTime)betDetail.transaction_time),
                    Stake = gameType == (int)TpGameType.SBSW_NumberGame ? betNumberDetail.stake * tpCurrency.ExchangeRate : betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = gameType == (int)TpGameType.SBSW_NumberGame ? betNumberDetail.winlost_amount * tpCurrency.ExchangeRate : betDetail.winlost_amount * tpCurrency.ExchangeRate,
                },
                TpCategory.VirtualSports => new VirtualSportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = betVirtualSportDetail?.trans_id,
                    TpUserName = betVirtualSportDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betVirtualSportDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betVirtualSportDetail?.match_datetime, (DateTime)betVirtualSportDetail?.transaction_time),
                    Stake = betVirtualSportDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betVirtualSportDetail.winlost_amount * tpCurrency.ExchangeRate,
                    Odds = betVirtualSportDetail.odds,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        private DateTime GetUTCTime(DateTime? target, DateTime defaultTime)
        {
            return new DateTimeOffset(target ?? defaultTime, new TimeSpan(-4, 0, 0))
                .ToOffset(new TimeSpan(0, 0, 0)).DateTime;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpBetRecordDetail = TpBetRecordProvider.GetTpBetRecordDetail(betRecordInfo.TpId, betRecordInfo.GameType, betRecordInfo.TransId, DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime);

            if (tpBetRecordDetail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var model = JsonConvert.DeserializeObject<DetailModel>(tpBetRecordDetail.Detail);
            var betDetail = model?.BetDetails?.First();
            var betNumberDetail = model?.BetNumberDetails?.First();
            var betVirtualSportDetail = model?.BetVirtualSportDetails?.First();

            if (betDetail == null && betNumberDetail == null && betVirtualSportDetail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            if (betDetail != null)
            {
                int.TryParse(betDetail.odds_type, out int odds_type);
                decimal.TryParse(betDetail.odds, out decimal odds);

                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = GetGameID(tpGameType, betDetail.sport_type),
                    Game = GetGameName(betDetail.sportname?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty),
                    RefNo = betDetail.parlay_ref_no ?? string.Empty,
                    Title = betDetail.leaguename?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty,
                    MatchID = betDetail.match_id ?? string.Empty,
                    BetType = betDetail.bettypename?.FirstOrDefault(i => i.lang == "en")?.name ?? string.Empty,
                    BetChoice = betDetail.bet_team ?? string.Empty,
                    Result = betDetail.ticket_status,
                    DeviceType = GetDeviceType(betDetail.betfrom),
                    IP = string.Empty,
                    Odds = odds,
                    OddsTypeEnum = GetOddsType(odds_type),
                    Live = betDetail.islive ?? string.Empty
                };
            }
            else if (betNumberDetail != null)
            {
                int.TryParse(betNumberDetail.odds_type, out int odds_type);
                decimal.TryParse(betNumberDetail.odds, out decimal odds);
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = betNumberDetail.sport_type.ToString(),
                    Game = tpGameType.TypeName,
                    RefNo = betNumberDetail.trans_id.ToString(),
                    Title = tpGameType.TypeName,
                    MatchID = betNumberDetail.match_id.ToString(),
                    BetType = betNumberDetail.bet_type.ToString(),
                    BetChoice = betNumberDetail.bet_team ?? string.Empty,
                    Result = betNumberDetail.ticket_status,
                    DeviceType = GetDeviceType(betNumberDetail.betfrom),
                    IP = string.Empty,
                    Odds = odds,
                    OddsTypeEnum = GetOddsType(odds_type),
                    Live = betNumberDetail.islive ?? string.Empty
                };
            }
            else
            {
                int.TryParse(betVirtualSportDetail.odds_type, out int odds_type);
                decimal.TryParse(betVirtualSportDetail.odds, out decimal odds);
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = betVirtualSportDetail.sport_type.ToString(),
                    Game = tpGameType.TypeName,
                    RefNo = betVirtualSportDetail.parlay_ref_no.ToString(),
                    Title = tpGameType.TypeName,
                    MatchID = betVirtualSportDetail.match_id.ToString(),
                    BetType = betVirtualSportDetail.bet_type.ToString(),
                    BetChoice = betVirtualSportDetail.bet_team ?? string.Empty,
                    Result = betVirtualSportDetail.ticket_status,
                    DeviceType = GetDeviceType(betVirtualSportDetail.betfrom),
                    IP = string.Empty,
                    Odds = odds,
                    OddsTypeEnum = GetOddsType(odds_type),
                    Live = betVirtualSportDetail.islive ?? string.Empty
                };
            }
        }

        private string ChangeTransIdToString(string json)
        {
            var jObject = JObject.Parse(json);
            if (jObject["BetDetails"] != null)
            {
                jObject["BetDetails"][0]["trans_id"] = jObject["BetDetails"][0]["trans_id"].ToString();
            }

            if (jObject["BetNumberDetails"] != null)
            {
                jObject["BetNumberDetails"][0]["trans_id"] = jObject["BetNumberDetails"][0]["trans_id"].ToString();
            }

            if (jObject["BetVirtualSportDetails"] != null)
            {
                jObject["BetVirtualSportDetails"][0]["trans_id"] = jObject["BetVirtualSportDetails"][0]["trans_id"].ToString();
            }
            return jObject.ToString();
        }
        private DeviceType GetDeviceType(string device)
        {
            switch (device)
            {
                case "m":
                case "l":
                case "c":
                case "2":
                case "5":
                case "%":
                case "@":
                case "]":
                case "7":
                case "~":
                    return DeviceType.Mobile;
                case "1":
                case "4":
                case "[":
                case "9":
                case ">":
                    return DeviceType.Desktop;
                default:
                    return DeviceType.None;
            }
        }

        private static readonly Dictionary<string, SportType> GameNameMappings = new Dictionary<string, SportType>
        {
            //各三方的Sport名稱先以SBSW為主，並建立SportType Enum以供轉換
            //額外轉換以下4項
            { "SBSW Virtual Sports", SportType.VirtualSports },
            { "SBSW Virtual Sports 2", SportType.VirtualSports },
            { "SBSW Number Game", SportType.NumberGame },
            { "E-Sports", SportType.ESports }
        };

        private static string GetGameName(string gameName)
        {
            return GameNameMappings.TryGetValue(gameName, out var sportType) ? Enum.GetName(typeof(SportType), sportType) : gameName;
        }

        private OddsTypeEnum GetOddsType(int oddsType)
        {
            return oddsType switch
            {
                1 => OddsTypeEnum.MalayOdds,
                2 => OddsTypeEnum.ChinaOdds,
                3 => OddsTypeEnum.DecimalOdds,
                4 => OddsTypeEnum.IndoOdds,
                5 => OddsTypeEnum.AmericanOdds,
                _ => OddsTypeEnum.None,
            };
        }

        private string GetGameID(TpGameTypeOption tpGameType, string sportType)
        {
            return tpGameType.Type == (int)TpGameType.SBSW_Vgaming ? "171" : sportType;
        }

        private GetGameDetailResult GetGameDetail(SabaConfig config, string matchid)
        {
            try
            {
                var apiUrl = $"{config.ApiUrl}/GetGameDetail";
                var sParam = $"vendor_id={config.VendorId}&match_ids={matchid}";
                var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 3);
                var result = FromJson<GetGameDetailResult>(json);
                return result;
            }
            catch
            {
                return default;
            }
        }

        #region SBSW app use: token 操作

        public ApiResult GetToken(TpCustInfo tpCustInfo)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var apiUrl = $"{config.ApiUrl}/GetToken";
            var sParam = $"vendor_id={config.VendorId}&vendor_member_id={tpCustInfo.TpUserName}";
            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 3);
            var result = FromJson<GetTokenResult>(json);

            if (result.error_code != 0)
            {
                return new ApiResult()
                {
                    RawMessage = result.message,
                    RawErrorCode = result.error_code.ToString(),
                    ErrorCode = ErrorCode.Fail
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = result.Data,
                 RawErrorCode = result.error_code.ToString(),
                RawMessage = result.message,
            };
        }

        public ApiResult InvalidateToken(TpCustInfo tpCustInfo)
        {
            var config = siteSabaConfigs[tpCustInfo.SiteId];
            var apiUrl = $"{config.ApiUrl}/InvalidateToken";
            var sParam = $"vendor_id={config.VendorId}&vendor_member_id={tpCustInfo.TpUserName}";
            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 3);
            var result = FromJson<Result>(json);

            if (result.error_code != 0)
            {
                return new ApiResult()
                {
                    RawMessage = result.message,
                    RawErrorCode = result.error_code.ToString(),
                    ErrorCode = ErrorCode.Fail
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                RawErrorCode = result.error_code.ToString(),
                RawMessage = result.message,
            };
        }

        public ApiResult VerifyToken(int siteId, string token)
        {
            var config = siteSabaConfigs[siteId];
            var apiUrl = $"{config.ApiUrl}/VerifyToken";
            var sParam = $"vendor_id={config.VendorId}&token={token}";
            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 3);
            var result = FromJson<VerifyTokenResult>(json);

            if (result.error_code != 0)
            {
                return new ApiResult()
                {
                    RawMessage = result.message,
                    RawErrorCode = result.error_code.ToString(),
                    ErrorCode = ErrorCode.Fail
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = result.Data.vendor_member_id,
                RawErrorCode = result.error_code.ToString(),
                RawMessage = result.message,
            };
        }

        #endregion
    }
}
