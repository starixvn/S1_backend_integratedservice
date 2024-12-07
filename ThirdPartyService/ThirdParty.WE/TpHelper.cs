using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.WE
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, WEConfig> _siteConfigDic;
        private readonly IPayloadLogger _payloadLogService;
        private readonly Dictionary<string, string> _gameNameList;
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;
        private const string _testCurrency = "UUS";

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WEConfig>(TpEnum.WE.ToString());
            _payloadLogService = provider.GetRequiredService<IPayloadLogger>();
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.WE).ToDictionary(d => d.GameId, d => d.Name);
        }

        #region Request

        public class CommonReq
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public Payload Payload { get; set; }
        }

        public class Payload
        {
            public ProductInfo ProductInfo { get; set; }
            public CustInfo CustInfo { get; set; }
            public AgentInfo AgentInfo { get; set; }
            public string HomeUrl { get; set; }
            public Additional Additional { get; set; }
        }

        public class ProductInfo
        {
            public string GameType { get; set; }
            public string GameId { get; set; }
            public string AppType { get; set; }
        }

        public class CustInfo
        {
            public string OperatorCustomerId { get; set; }
            public string Language { get; set; }
            public string Platform { get; set; }
            public string CurrencyISO { get; set; }
            public string BrowserUserAgent { get; set; }
            public string IP { get; set; }
            public string WalletSideToken { get; set; }
        }

        public class AgentInfo
        {
            public string ExtraInfo { get; set; }
        }

        public class Additional
        {
            public string SpecialKey { get; set; }
        }

        #endregion Request

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
            ApiResult oApiResult = new()
            {
                ErrorCode = ErrorCode.Success
            };
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
                    config.GameType,
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
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransLC_WE_TicketNo_Get", new { YYYYMM, TicketNo = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var GameId = detailList.GetFirstStringValue("GameId");
            var GameRoundId = detailList.GetFirstStringValue("GameRoundId");
            var IpAddress = detailList.GetFirstStringValue("IpAddress");
            var BetChoice = detailList.GetFirstStringValue("BetChoice");
            var GameResult = detailList.GetFirstStringValue("GameResult");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = GameId,
                Game = GetGameName(GameId),
                RefNo = GameRoundId,
                Title = tpGameType.TypeName,
                MatchID = string.Empty,
                BetType = string.Empty,
                BetChoice = BetChoice,
                Result = GameResult,
                DeviceType = DeviceType.None,
                IP = IpAddress,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var ApiUrl = $"{config.ApiUrl}/api/CommonWallet/LaunchGame";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "0";
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string betLimit = inputs.ContainsKey("betLimitGroup") && !string.IsNullOrEmpty(inputs["betLimitGroup"]) ? inputs["betLimitGroup"] : "A5";

            var key = ApplyToken(tpCustInfo);

            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    ProductInfo = new
                    {
                        config.GameType,
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
                        WalletSideToken = key
                    },
                    HomeUrl = homeUrl,
                    Additional = new
                    {
                        BetLimit = betLimit
                    }
                }
            };
            var postdata = JsonConvert.SerializeObject(input, Formatting.None);
            var header = new Dictionary<string, string>()
            {
                { "operator-pwd", config.OperatorPwd},
                { "operator-sign", Encrypt(postdata, config)},
            };
            _payloadLogService.LogDetailedPayload(postdata, new PayloadLogMeta()
            {
                GameType = nameof(WE),
                Action = nameof(Login)
            });
            var result = HttpPost(ApiUrl, postdata, "application/json", header);

            _payloadLogService.UpdateDetailedPayLoadWithResponse(postdata, result);
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

        private string Encrypt(string req, WEConfig config)
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

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return gameCode;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return (dic != null) ? dic.TryGetValue("en", out var result) ? result : gameCode : gameCode;
        }
    }
}