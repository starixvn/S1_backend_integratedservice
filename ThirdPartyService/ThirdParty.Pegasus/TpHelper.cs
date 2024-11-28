using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Pegasus
{
    internal class TpHelper : ThirdPartyHelperBase
    {
        private const string ContentType = "application/json";
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.Pegasus.ToString());
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Pegasus).ToDictionary(d => d.AppId, d => d.Name);
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";

            var siteInputs = _siteConfigDic[tpCustInfo.SiteId];
            var config = new PegasusConfig(siteInputs, tpCustInfo.TpCurrencyId);

            var token = ApplyToken(tpCustInfo);
            var sessionKeyArr = new[] { token, tpCustInfo.TpUserName};
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            var request = new
            {
                playerUniqueID = fun is "0" ? tpCustInfo.TpUserName : "fun",
                sessionToken = sessionKey,
                merchantID = config.MerchantID,
                merchantSecretKey = config.SecretKey,
                lang = TpLangId,
                gameID = gameCode,
                isTrialModeV2 = fun is "0" ? bool.FalseString : bool.TrueString,
                backUrl = homeUrl,
                userDevice = appType is AppType.Desktop ? "1" : "2", //PC=1 , H5=2
                version = "v1",
            };

            var dicRequest = FromJson<Dictionary<string, string>>(ToJson(request));
            var parameters = string.Join("&", dicRequest.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var gameUrl = config.ApiUrl + "/game/launch-link";
            var json = HttpGet(gameUrl, parameters);
            var result = FromJson<PegasusGameLaunchResult>(json);

            if (result.code != 0)
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.code.ToString(),
                    RawMessage = result.message,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = result.data.link,
            };
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_Pegasus_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);

            var betTime = (DateTime) wagerDetail.GetValueOrDefault("BetTime", DateTime.MinValue);
            var partitionTime = betTime.ToString("yyyyMM");
            var settleDetail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_Pegasus_Settle_Get", new { getBetDetail.TransId , YYYYMM = partitionTime }, DataBase.Main);
            
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Slots => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = "",
                    TpUserName = wagerDetail.GetValueOrDefault("PlayerName")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("BetTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)settleDetail.GetValueOrDefault("SettlementTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("BetAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("WinLossAmount") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("GameID")?.ToString(),
                    GameName = GetGameName(wagerDetail.GetValueOrDefault("GameID")?.ToString()) 
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_Pegasus_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            if (!wagerDetail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameID = wagerDetail.GetValueOrDefault("GameID")?.ToString();
            var result = new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameID,
                Game = GetGameName(gameID),
                RefNo = "",
                Title = tpGameType.TypeName,
                MatchID = wagerDetail.GetValueOrDefault("RoundID")?.ToString(),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
            
            return result;
        }

        #region Transfer Methods

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region private

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        #endregion
    }
}
