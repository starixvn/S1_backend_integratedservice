using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PlayTech
{
    public class TpHelper : ThirdPartyHelperBase
    {
        #region PlayTech Configs Setting

        private class TpConfig
        {
            public string ApiUrl { get; set; }

            public string GameUrl { get; set; }

            public string BrandId { get; set; }

            public string BrandName { get; set; }

            public string SecretKey { get; set; }

            public TpConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception("ApiUrl");
                GameUrl = inputs.GetValueOrDefault("GameUrl") ?? throw new Exception("GameUrl");
                BrandId = inputs.GetValueOrDefault("BrandId") ?? throw new Exception("BrandId");
                BrandName = inputs.GetValueOrDefault("BrandName") ?? throw new Exception("BrandName");
                SecretKey = inputs.GetValueOrDefault("SecretKey") ?? throw new Exception("SecretKey");
            }
        }

        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper()
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.PlayTech).ToDictionary(d => d.AppId, d => d.Name);
        }

        #endregion

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
            ApiResult<UserBalance> oApiResult = new()
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

            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_PlayTech_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            if (wagerDetail == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }

            var detailUrl = wagerDetail.GetValueOrDefault("detailUrl")?.ToString();
            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = detailUrl,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_PlayTech_Detail_Get",
                new { betRecordInfo.TransId }, DataBase.Wager);

            if (!wagerDetail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            string gameCode = wagerDetail.GetStringValue("gameCode");
            string roundId = wagerDetail.GetStringValue("roundId");
            string platform = wagerDetail.GetStringValue("platform");

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = roundId,
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(platform),
                    IP = string.Empty,
                },
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = roundId,
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(platform),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var tpConfig = new TpConfig(inputs);
            string gameId = inputs.GetValueOrDefault("gameId") ?? throw new Exception("gameId");
            int tpGameType = (gameId == "0" || gameId == "" || inputs.GetValueOrDefault("tpGameType") == TpGameType.PlayTech_LiveCasino.ToString()) ? (int)TpGameType.PlayTech_LiveCasino : (int)TpGameType.PlayTech_RNG;
            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            string homeUrl = inputs.GetValueOrDefault("homeUrl") ?? "";
            string token = inputs.GetValueOrDefault("Token") ?? throw new Exception("Token");

            var platform = (appType != AppType.Desktop) ? "mobile" : "web";
            var mode = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            var sessionKeyArr = new string[] { ApplyToken(tpCustInfo), token, tpCustInfo.TpUserName, tpGameType.ToString() };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            gameId = (tpGameType == (int)TpGameType.PlayTech_LiveCasino) ? "ubal" : gameId;
            /*
             * 對於 BRL 貨幣
             * 必須使用 PTN 遊戲代碼才能存取遊戲
             * 測試環境和正式環境使用的遊戲程式碼不同
             */
            if (!IsDevelopment && tpCustInfo.TpCurrencyId == "BRL")
            {
                // 套用ptn_，讓SlotGame不用多設定，直接使用原資料
                gameId = $"ptn_{gameId}";
            }

            ApiResult oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{tpConfig.GameUrl}/launcher?gameCode={gameId}&token={HttpUtility.UrlEncode(sessionKey)}&platform={platform}&language={TpLangId}&playerId={tpCustInfo.TpUserName}&brandId={tpConfig.BrandId}&mode={mode}&backUrl={homeUrl}"
            };

            return oApiResult;
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
            return (dic != null) ? dic.TryGetValue("en", out var result) ? result : string.Empty : string.Empty;
        }

        private DeviceType GetDeviceType(string platform)
        {
            DeviceType deviceType;
            switch (platform)
            {
                case "web":
                    deviceType = DeviceType.Desktop;
                    break;
                case "mobile":
                    deviceType = DeviceType.Mobile;
                    break;
                default:
                    deviceType = DeviceType.None;
                    break;
            }

            return deviceType;

        }
    }
}