using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.One789
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly One789Config _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<One789Config>(TpEnum.One789.ToString());
        }

        #region Response

        private class ApiResponse
        {
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string Token { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
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
            var config = _siteConfig;

            string ApiUrl = config.ApiUrl;
            string ApiKey = config.ApiKey;
            string GameUrl = config.GameUrl;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");

            ApiUrl = $"{ApiUrl}/auth/sign-in";
            var Login = new
            {
                Username = tpCustInfo.TpUserName
            };
            string InputJson = JsonConvert.SerializeObject(Login);

            Dictionary<string, string> headerReq = new()
            {
                { "x-one-api-key", HttpUtility.UrlEncode(ApiKey) }
            };

            var result = HttpPost(
                ApiUrl,
                InputJson,
                "application/json",
                headerReq
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (!string.IsNullOrEmpty(json.Token))
                {
                    string App = (appType == AppType.Native) ? "&app=1" : "";
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{GameUrl}?token={HttpUtility.UrlEncode(json.Token)}&production={gameId}{App}";
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
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_One789_Detail_Get",
                new { getBetDetail.TransId, GameType = getBetDetail.TpGameTypeOption.Type }, DataBase.Wager);
            if (wagerDetail == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }

            var detail = ToJson(wagerDetail);

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Lottery => new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("Term")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("Username")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("CreatedAt", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("UpdatedAt", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("Amount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLose") * tpCurrency.ExchangeRate,
                },
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("GameNumber")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("Username")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("CreatedAt", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("UpdatedAt", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("Amount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLose") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("GameNumber")?.ToString(),
                    GameName = string.Empty,
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_One789_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type }, DataBase.Wager);

            return tpGameType.CategoryId switch
            {
                TpCategory.Lottery => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("GameType"),
                    Game = tpGameType.TypeName,
                    RefNo = detail.GetStringValue("Term"),
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = detail.GetStringValue("IP"),
                },
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = string.Empty,
                    Game = tpGameType.TypeName,
                    RefNo = detail.GetStringValue("GameNumber"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("GameRound"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("GameResult"),
                    DeviceType = DeviceType.None,
                    IP = detail.GetStringValue("IP"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }
    }
}
