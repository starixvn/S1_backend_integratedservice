using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.JILI
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly TemporaryLogSentryService temporaryLogSentryService;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, JILIConfig> _siteConfigDic;
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;
        private const string _testCurrency = "UUS";

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.JILI).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<JILIConfig>(TpEnum.JILI.ToString());
            temporaryLogSentryService = provider.GetRequiredService<TemporaryLogSentryService>();
        }

        #region Response

        public class LoginResp
        {
            public string Seq { get; set; }
            public DateTime TimeStamp { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public Payload_Login Payload { get; set; }
        }

        public class Payload_Login
        {
            public string ReturnUrl { get; set; }
        }

        public class GetBetHistoryResp : LoginResp
        {
        }

        #endregion Response

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
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
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var ApiUrl = $"{config.ApiUrl}/api/CommonWallet/LaunchGame";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var key = ApplyToken(tpCustInfo);

            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    ProductInfo = new
                    {
                        GameType = config.GameType,
                        GameId = gameId,
                        AppType = (appType == AppType.Desktop) ? "D" : "M",
                    },
                    CustInfo = new
                    {
                        OperatorCustomerId = tpCustInfo.TpUserName,
                        OperatorCustomerName = tpCustInfo.TpUserName,
                        Language = TpLangId,
                        Platform = (appType == AppType.Desktop) ? "D" : "M",
                        CurrencyISO = tpCustInfo.IsTest ? _testCurrency : tpCustInfo.TpCurrencyId,
                        IpAddress = inputs["ClientIP"],
                        WalletSideToken = key,
                    },
                    HomeUrl = homeUrl,
                }
            };
            var postdata = JsonConvert.SerializeObject(input, Formatting.None);
            var header = new Dictionary<string, string>()
            {
                { "operator-pwd", config.OperatorPwd},
                { "operator-sign", Encrypt(postdata, config)},
            };

            var result = HttpPost(ApiUrl, postdata, "application/json", header);

            temporaryLogSentryService.WriteLogLaunchGame(nameof(JILI), ApiUrl, header, postdata, result);

            ApiResult oApiResult = new ApiResult();


            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResp>(result);
                if (json.ErrorCode == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.Payload.ReturnUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.ErrorMessage;
                    oApiResult.RawErrorCode = json.ErrorCode.ToString();
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
            var config = _siteConfigDic[tpCustInfo.SiteId];

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var convert = new DateTimeOffset(getBetDetail.SettledTime);
            var time = convert.ToOffset(new TimeSpan(_utc, 0, 0)).ToString("yyyy/MM/dd HH:mm:ss");
            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    GameType = config.GameType,
                    OperatorCustId = tpCustInfo.TpUserName,
                    TicketNo = getBetDetail.TransId,
                    SettleDate = time,
                    Language = "en-US",
                    AgentInfo = new
                    {
                        AgentKey = tpCustInfo.TpCurrencyId
                    }
                }
            };

            var ApiUrl = $"{config.ApiUrl}/api/CommonWallet/GetBetHistory";
            var postdata = JsonConvert.SerializeObject(input, Formatting.None);
            var header = new Dictionary<string, string>()
            {
                { "operator-pwd", config.OperatorPwd},
                { "operator-sign", Encrypt(postdata, config)},
            };

            var result = HttpPost(ApiUrl, postdata, "application/json", header);
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<GetBetHistoryResp>(result);
                if (json.ErrorCode == 0)
                {
                    return new BaseDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        DetailUrl = json.Payload.ReturnUrl,
                        TpCurrency = tpCurrency,
                    };
                }
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                TpCurrency = tpCurrency,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_JILI_TicketNo_Get", new { YYYYMM, TicketNo = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var GameId = detailList.GetFirstStringValue("game");
            var GameRoundId = detailList.GetFirstStringValue("GameRoundId");
            var IpAddress = detailList.GetFirstStringValue("IpAddress");

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = GameId,
                    Game = GetGameName(GameId),
                    RefNo = GameRoundId,
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = IpAddress,
                },
                TpCategory.Fishing => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = GameId,
                    Game = GetFishGameName(GameId),
                    RefNo = GameRoundId,
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = IpAddress,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
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

        private static string GetFishGameName(string gameCode)
        {
            return gameCode switch
            {
                "1" => "Royal Fishing",
                "2" => "Bombing Fishing",
                "3" => "Dinosaur Tycoon",
                "4" => "Jackpot Fishing",
                "5" => "Dragon Fortune",
                "6" => "Mega Fishing",
                "7" => "Boom Legend",
                "8" => "Happy Fishing",
                "9" => "All-star Fishing",
                _ => gameCode,
            };
        }
        private string Encrypt(string req, JILIConfig config)
        {
            var s = req + config.OperatorKey;
            var sign = ToMD5(s);
            return sign;
        }

        private static string GetCurrentTimeFormat()
        {
            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            return currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime);
        }
    }
}