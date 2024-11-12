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
using ThirdPartyCore.Model.TpBetRecordExtend;

public class PGConfig : TpSiteConfig
    {
    public string OperatorPwd { get; set; }
    public string OperatorKey { get; set; }
    public string GameType { get; set; }
}

namespace ThirdParty.PG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, PGConfig> _siteConfigDic;
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;
        private const string _testCurrency = "UUS";

        private readonly IPayloadLogger _temporaryLogPayloadService;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PGConfig>(TpEnum.PG.ToString());
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

        public class BettransData_PG
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
            var spName = GetSPNameBasedOnTpGameType();
            var detailList = TpBetRecordProvider.DbQuery(spName, new { YYYYMM, TicketNo = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var GameId = detailList.GetFirstStringValue("GameId");
            var GameRoundId = detailList.GetFirstStringValue("GameRoundId");
            var IpAddress = detailList.GetFirstStringValue("IpAddress");
            var details = JsonConvert.DeserializeObject<IList<BettransData_PG>>(JsonConvert.SerializeObject(detailList));
            var resultId = details.Where(i => i.TicketType == 4).FirstOrDefault()?.ResultId;

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = GameId,
                Game = GameId,
                RefNo = GameRoundId,
                Title = tpGameType.TypeName,
                MatchID = resultId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = IpAddress,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            string gameId = 
                inputs.GetValueOrDefault("gameId", "");
            string homeUrl =
                inputs.GetValueOrDefault("homeUrl", ""); 
            var gameTypeSpecificApiUrl = config.ApiUrl;
            var ApiUrl = $"{gameTypeSpecificApiUrl}/api/CommonWallet/LaunchGame";
            var gameType = config.GameType;
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
                        GameId = "6001",
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

        private string Encrypt(string req, PGConfig config)
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

        private static string GetSPNameBasedOnTpGameType()
        {
            return "Service_BettransRng_PG_TicketNo_Get";
        }
    }
}