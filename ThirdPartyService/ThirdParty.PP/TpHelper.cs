using System.Text.RegularExpressions;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, PPConfig> _siteConfigDic;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;
        private const string _testCurrency = "UUS";

        private const string PPLiveCasinoGameTypeId = "16";

        private readonly IPayloadLogger _temporaryLogPayloadService;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.PP).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PPConfig>(TpEnum.PP.ToString());
            _temporaryLogPayloadService = provider.GetRequiredService<IPayloadLogger>();
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

        public class BettransData_PP
        {
            public string TicketNo { get; set; }
            public string GameRoundId { get; set; }
            public string ResultId { get; set; }
            public int TicketType { get; set; }
        }

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

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var spName = GetSPNameBasedOnTpGameType(betRecordInfo.GameType);
            var detailList = TpBetRecordProvider.DbQuery(spName, new { YYYYMM, TicketNo = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var GameId = detailList.GetFirstStringValue("gameId");
            var GameRoundId = detailList.GetFirstStringValue("roundId");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = GameId,
                Game = GetGameName(GameId, betRecordInfo.GameType),
                RefNo = GameRoundId,
                Title = tpGameType.TypeName,
                MatchID = string.Empty,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            string gameId = 
                inputs.GetValueOrDefault("gameId", "");
            string homeUrl =
                inputs.GetValueOrDefault("homeUrl", ""); 
            string tpGameType = 
                inputs.GetValueOrDefault("tpGameType", "0");
            var gameTypeSpecificApiUrl = GetEndpointBasedOnTpGameType(tpGameType, config);
            var ApiUrl = $"{gameTypeSpecificApiUrl}/api/CommonWallet/LaunchGame";
            var gameType = GetGameTypeBasedOnTpGameType(tpGameType, config);
            var key = ApplyToken(tpCustInfo);

            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    ProductInfo = new
                    {
                        GameType = gameType,
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
                        IpAddress = inputs.GetValueOrDefault("ClientIP", ""),
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
            _temporaryLogPayloadService.LogDetailedPayload(postdata, new PayloadLogMeta() {
                GameType = gameType
            });
            var result = HttpPost(ApiUrl, postdata, "application/json", header);
            _temporaryLogPayloadService.UpdateDetailedPayLoadWithResponse(postdata, result);

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

        private string Encrypt(string req, PPConfig config)
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

        private static string GetEndpointBasedOnTpGameType(string gameType, PPConfig config) {
            if (gameType == PPLiveCasinoGameTypeId) {
                return config.LiveCasinoApiUrl;
            }

            return config.ApiUrl;
        }

        private static string GetGameTypeBasedOnTpGameType(string gameType, PPConfig config) {
            if (gameType == PPLiveCasinoGameTypeId)
            {
                return config.LiveCasinoGameType;
            }

            return config.GameType;
        }

        private static string GetSPNameBasedOnTpGameType(int  gameType)
        {
            if (gameType.ToString() == PPLiveCasinoGameTypeId)
            {
                return "Service_BettransLC_PP_TicketNo_Get";
            }

            return "Service_BettransRng_PP_TicketNo_Get";
        }

        private string GetGameName(string gameId, int gameType)
        {
            if (gameType.ToString() == PPLiveCasinoGameTypeId)
            {
                return gameId;
            }

            if (!_gameNameList.TryGetValue(gameId, out var name))
            {
                return gameId;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : gameId;
        }
    }
}