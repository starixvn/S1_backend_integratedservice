using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Amigo
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, AmigoConfig> _siteConfigDic;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Amigo).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AmigoConfig>(TpEnum.Amigo.ToString());
        }

        public class BettransData_Amigo
        {
            public string userId { get; set; }
            public string gameId { get; set; }
            public long roundId { get; set; }
            public string reference { get; set; }
            public long timestamp { get; set; }
            public string roundDetails { get; set; } = "";
            public string bonusCode { get; set; } = "";
            public long jackpotId { get; set; }
        }

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
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var YYYYMM = getBetDetail.SettledTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Amigo_Get", new { YYYYMM, roundId = getBetDetail.TransId }, DataBase.Main);
            if (!detailList.Any() || detailList == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }
            var detail = ToJson(detailList);
            string gameCode = detailList.FirstOrDefault()?.GetValueOrDefault("gameId")?.ToString();
            var stake = detailList.Where(i => (sbyte)i.GetValueOrDefault("TicketType") == 1 && (string)i.GetValueOrDefault("bonusCode") == "").Sum(i => Math.Abs((decimal)i.GetValueOrDefault("amount")));
            var payout = detailList.Where(i => (sbyte)i.GetValueOrDefault("TicketType") == 4).Sum(i => (decimal)i.GetValueOrDefault("amount"));
            var details = JsonConvert.DeserializeObject<IList<BettransData_Amigo>>(JsonConvert.SerializeObject(detailList));

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = getBetDetail.TransId.ToString(),
                    TpUserName = detailList.FirstOrDefault()?.GetValueOrDefault("userId")?.ToString(),
                    UTCBetTime = details.Min(i => DateTimeOffset.FromUnixTimeMilliseconds(i.timestamp).DateTime),
                    UTCSettledTime = details.Max(i => DateTimeOffset.FromUnixTimeMilliseconds(i.timestamp).DateTime),
                    Stake = stake,
                    WinlostAmount = payout - stake,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Amigo_Get", new { YYYYMM, roundId = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            var details = JsonConvert.DeserializeObject<IList<BettransData_Amigo>>(JsonConvert.SerializeObject(detailList));
            string gameCode = detailList.FirstOrDefault()?.GetValueOrDefault("gameId")?.ToString();
            var roundId = detailList.FirstOrDefault()?.GetValueOrDefault("roundId")?.ToString();
            var jackpotId = details.Max(x => x.jackpotId).ToString();
            var bonusCode = detailList.FirstOrDefault()?.GetValueOrDefault("bonusCode")?.ToString();

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = roundId,
                    Title = tpGameType.TypeName,
                    MatchID = jackpotId,
                    BetType = string.Empty,
                    BetChoice = bonusCode,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var styleName = config.StyleName;
            var secretKey = config.SecretKey;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var loginApiUrl = $"{apiUrl}/playGame.do";

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                string funGameUrl = $"{config.FunGameUrl}/api/link/id/{styleName}/?gameSymbol={gameId}&lobbyurl={HttpUtility.UrlEncode(homeUrl)}";
                string sContent = HttpGet(funGameUrl, "", "text/html; charset=UTF-8");
                return string.IsNullOrEmpty(sContent)
                    ? new ApiResult()
                    {
                        ErrorCode = ErrorCode.Fail,
                        Message = sContent
                    }
                    : new ApiResult()
                    {
                        ErrorCode = ErrorCode.Success,
                        Message = sContent
                    };
            }

            // insert uuid and token mapping
            var token = ApplyToken(tpCustInfo);
            var tpToken = Guid.NewGuid().ToString("N").ToUpper();
            UpdateTpToken(token, tpToken);

            // key - UrlEncode list of parameters
            var parameters = new Dictionary<string, string>()
            {
                 { "token", tpToken },
                 { "symbol", gameId },
                 { "lobbyUrl", homeUrl }
            };
            var sParam = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{loginApiUrl}?"
                        + $"stylename={styleName}"
                        + $"&key={HttpUtility.UrlEncode(sParam)}"
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
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