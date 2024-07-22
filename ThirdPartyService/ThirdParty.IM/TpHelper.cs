using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.IM
{
    public partial class TpHelper : ThirdPartyHelperBase
    {
        #region IM Configs Setting

        Dictionary<TpGameType, int> _productWallet = new()
        {
            { TpGameType.IM_Sports, (int)ProductWallet.IMSB },
            { TpGameType.IM_ESports, (int)ProductWallet.ESportsbull },
        };

        private readonly Dictionary<int, IMConfig> _siteIMConfig;
        #endregion

        private const string ContentType = "application/json";
        private readonly WebProxy? _proxy;

        public TpHelper(IServiceProvider provider)
        {
            var tokyoProxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ProxyConfig>("TokyoProxy");
            if (tokyoProxyConfig != null)
            {
                _proxy = new WebProxy(tokyoProxyConfig.ProxyHost, tokyoProxyConfig.ProxyPort);
            }

            _siteIMConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<IMConfig>(TpEnum.IM.ToString());
        }

        public class IMConfig : TpSiteConfig 
        {
            public string Key { get; set; }
            public string Prefix { get; set; }
            public string MerchantName { get; set; }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteIMConfig[tpCustInfo.SiteId];
            var apiUrl = $"{config.ApiUrl}/Player/Register";

            var signUp = new RegisterModel
            {
                MerchantCode = config.Key,
                PlayerId = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId,
                Password = tpCustInfo.TpPwd
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy);
            var result = FromJson<RegisterResponseModel>(json);

            if (result.Code == 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = result.Message
                };
            }
            else
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = result.Message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteIMConfig[tpCustInfo.SiteId];
            var apiUrl = appType == AppType.Desktop ? $"{config.ApiUrl}/Game/NewLaunchGame" : $"{config.ApiUrl}/Game/NewLaunchMobileGame";
            var playerName = tpCustInfo.TpUserName;

            var tpGameType = inputs.ContainsKey("tpGameType") && !string.IsNullOrEmpty(inputs["tpGameType"]) ? Convert.ToInt32(inputs["tpGameType"]) : (int)TpGameType.IM_Sports;
            var productWallet = _productWallet[(TpGameType)tpGameType];

            var signUp = new LaunchGameModel
            {
                MerchantCode = config.Key,
                PlayerId = playerName,
                GameCode = inputs["gameId"],
                Language = TpLangId,
                IpAddress = inputs["ClientIP"],
                ProductWallet = productWallet,
                LobbyURL = inputs["homeUrl"]
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy, null);
            var result = FromJson<LaunchGameResponseModel>(json);

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = result.GameUrl,
                RawMessage = result.Message
            };
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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

            IReadOnlyDictionary<string, object>? wagerDetail = null;
            var detailSportModel = new WagerDetailSport();
            var detailESportModel = new WagerDetailESport();

            switch (getBetDetail.TpGameTypeOption.CategoryId)
            {
                case TpCategory.Sports:
                    wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerSport_IM_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
                    if (wagerDetail == null)
                    {
                        throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
                    }

                    var wagerSportDetailsArray = JsonConvert.DeserializeObject<IEnumerable<WagerDetailSport>>(wagerDetail.GetValueOrDefault("DetailItems") as string);
                    if (wagerSportDetailsArray.Any())
                    {
                        detailSportModel = wagerSportDetailsArray.FirstOrDefault();
                    }
                    break;

                case TpCategory.Esports:
                    wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerESport_IM_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
                    if (wagerDetail == null)
                    {
                        throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
                    }

                    var wagerESportDetailsArray = JsonConvert.DeserializeObject<IEnumerable<WagerDetailESport>>(wagerDetail.GetValueOrDefault("DetailItems") as string);
                    if (wagerESportDetailsArray.Any())
                    {
                        detailESportModel = wagerESportDetailsArray.FirstOrDefault();
                    }
                    break;
            }

            var detail = ToJson(wagerDetail);

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    TpUserName = wagerDetail.GetValueOrDefault("PlayerId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("WagerCreationDateTime", DateTime.MinValue),
                    UTCSettledTime = (wagerDetail.GetValueOrDefault("SettlementDate") as DateTime?) ?? DateTime.MinValue,
                    Odds = detailSportModel?.Odds.ToString(),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("StakeAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLoss") * tpCurrency.ExchangeRate,
                    LeagueName = detailSportModel?.EventName,
                    HomeTeamName = detailSportModel?.HomeTeamName,
                    AwayTeamName = detailSportModel?.AwayTeamName,
                    Handicap = detailSportModel?.Handicap,
                    TicketStatus = GetTicketStatus(wagerDetail.GetValueOrDefault("ResultStatus")?.ToString()),
                    AwayScore = detailSportModel?.WagerAwayTeamScore,
                    HomeyScore = detailSportModel?.WagerHomeTeamScore,
                    SelectionName = detailSportModel?.Selection
                },
                TpCategory.Esports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    TpUserName = wagerDetail.GetValueOrDefault("PlayerId")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("WagerCreationDateTime", DateTime.MinValue),
                    UTCSettledTime = (wagerDetail.GetValueOrDefault("SettlementDate") as DateTime?) ?? DateTime.MinValue,
                    Odds = detailESportModel?.Odds,
                    Stake = (decimal)wagerDetail.GetValueOrDefault("StakeAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLoss") * tpCurrency.ExchangeRate,
                    LeagueName = detailESportModel?.SportsName,
                    HomeTeamName = detailESportModel?.HomeTeamName,
                    AwayTeamName = detailESportModel?.AwayTeamName,
                    Handicap = detailESportModel?.Handicap,
                    TicketStatus = GetTicketStatus(wagerDetail.GetValueOrDefault("ResultStatus")?.ToString()),
                    AwayScore = detailESportModel?.WagerAwayTeamScore,
                    HomeyScore = detailESportModel?.WagerHomeTeamScore,
                    SelectionName = detailESportModel?.Selection,
                    EventName = detailESportModel?.EventName,
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
            IReadOnlyDictionary<string, object>? wagerDetail = null;
            var detailSportModel = new WagerDetailSport();
            var detailESportModel = new WagerDetailESport();

            switch (tpGameType.CategoryId)
            {
                case TpCategory.Sports:
                    wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerSport_IM_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
                    if (wagerDetail == null)
                    {
                        throw new BetDetailNotFoundException($"{betRecordInfo.TransId} bet detail not found.");
                    }

                    var wagerSportDetailsArray = JsonConvert.DeserializeObject<IEnumerable<WagerDetailSport>>(wagerDetail.GetValueOrDefault("DetailItems") as string);
                    if (wagerSportDetailsArray.Any())
                    {
                        detailSportModel = wagerSportDetailsArray.FirstOrDefault();
                    }
                    break;

                case TpCategory.Esports:
                    wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerESport_IM_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
                    if (wagerDetail == null)
                    {
                        throw new BetDetailNotFoundException($"{betRecordInfo.TransId} bet detail not found.");
                    }

                    var wagerESportDetailsArray = JsonConvert.DeserializeObject<IEnumerable<WagerDetailESport>>(wagerDetail.GetValueOrDefault("DetailItems") as string);
                    if (wagerESportDetailsArray.Any())
                    {
                        detailESportModel = wagerESportDetailsArray.FirstOrDefault();
                    }
                    break;
            }

            return tpGameType.CategoryId switch
            {
                TpCategory.Sports => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = wagerDetail.GetValueOrDefault("GameID")?.ToString(),
                    Game = GetGameName(detailSportModel?.SportsName),
                    RefNo = wagerDetail.GetValueOrDefault("BetId")?.ToString(),
                    Title = detailSportModel?.EventName,
                    MatchID = detailSportModel?.EventId,
                    BetType = detailSportModel?.BetType,
                    BetChoice = detailSportModel?.Selection,
                    Result = GetTicketStatus(wagerDetail.GetValueOrDefault("ResultStatus")?.ToString()),
                    DeviceType = GetDeviceType(wagerDetail.GetValueOrDefault("Platform")?.ToString()),
                    IP = string.Empty,
                    Odds = detailSportModel?.Odds ?? 0,
                    OddsTypeEnum = GetOddsType(wagerDetail.GetValueOrDefault("OddsType")?.ToString()),
                    Live = detailSportModel?.Market ?? String.Empty,
                },
                TpCategory.Esports => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = wagerDetail.GetValueOrDefault("GameID")?.ToString(),
                    Game = detailESportModel?.SportsName ?? "",
                    RefNo = wagerDetail.GetValueOrDefault("BetId")?.ToString(),
                    Title = detailESportModel?.EventName,
                    MatchID = detailESportModel?.EventId,
                    BetType = detailESportModel?.BetType,
                    BetChoice = detailESportModel?.Selection,
                    Result = GetTicketStatus(wagerDetail.GetValueOrDefault("ResultStatus")?.ToString()),
                    DeviceType = GetDeviceType(wagerDetail.GetValueOrDefault("Platform")?.ToString()),
                    IP = string.Empty,
                    Odds = decimal.TryParse(detailESportModel?.Odds, out var odds) ? odds : 0,
                    OddsTypeEnum = GetOddsType(wagerDetail.GetValueOrDefault("OddsType")?.ToString()),
                    Live = detailESportModel?.MarketType ?? String.Empty,
                }
            };

        }

        private string GetTicketStatus(string resultStatus)
        {
            return resultStatus switch
            {
                "1" => "win",
                "2" => "loss",
                "3" => "draw",
                "4" => "win half",
                "5" => "loss half",
                _ => ""
            };
        }

        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "Desktop" => DeviceType.Desktop,
                "Mobile" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }

        private OddsTypeEnum GetOddsType(string oddsType)
        {
            return oddsType switch
            {
                "HK" => OddsTypeEnum.HongKongOdds,
                "EURO" => OddsTypeEnum.DecimalOdds,
                "MALAY" => OddsTypeEnum.MalayOdds,
                "INDO" => OddsTypeEnum.IndoOdds,
                _ => OddsTypeEnum.None,
            };
        }

        private static readonly Dictionary<string, SportType> GameNameMappings = new Dictionary<string, SportType>
        {
            { "Soccer", SportType.Soccer },
            { "Basketball", SportType.Basketball },
            { "Volleyball", SportType.Volleyball },
            { "Badminton", SportType.Badminton },
            { "Tennis", SportType.Tennis }
        };

        private static string GetGameName(string gameName)
        {
            return GameNameMappings.TryGetValue(gameName, out var sportType)
                ? Enum.GetName(typeof(SportType), sportType)
                : gameName;
        }

    }
}
