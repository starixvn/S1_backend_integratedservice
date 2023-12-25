using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.AP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private const string _formatOfMini = "yyyy-MM-ddTHH:mm:ss";
        private const string _formatOfSlot = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private readonly List<string> _gameListOfMini;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, APConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameListOfMini = new List<string> { "Crash", "CardRacing" };
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.AP).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<APConfig>(TpEnum.AP.ToString());
        }

        public class GetPlayerTokenrep
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string Token { get; set; }
        }

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
            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            var Config = _siteConfigDic[tpCustInfo.SiteId];
            string GameUrl = Config.GameUrl;
            string SiteCode = Config.SiteCode;

            string LangId = TpLangId;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            bool FunPlay = fun == "1";

            ApiResult oApiResult = new();
            if (_gameListOfMini.Contains(gameId))
            {
                // mini game
                if (FunPlay)
                {
                    DefTpCurrencyId DefaultTpCurrencyId = ServerInfoProvider.GetDefaultTpCurrencyIdByTpId(tpCustInfo.SiteId, (int)TpEnum.AP);
                    tpCustInfo.TpUserName = "";
                    tpCustInfo.TpCurrencyId = (DefaultTpCurrencyId != null) ? DefaultTpCurrencyId.TpCurrencyId : "";
                }
                string URL = GetLoginOfMiniGame(tpCustInfo, TpLangId, Config, gameId, SiteCode);
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = URL;
            }
            else
            {
                string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&LangCode={LangId}&FunPlay={FunPlay}";
                string URL = $"{GameUrl}LaunchGame?{Params}";

                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = URL;
            }
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var Config = _siteConfigDic[tpCustInfo.SiteId];
            string GameUrl = Config.GameUrl;
            string ApiUrl = Config.ApiUrl;
            string SiteCode = Config.SiteCode;
            string GameRoundId = getBetDetail.TransId;

            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            // mini game
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_AP_Mini_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            if (wagerDetail != null && wagerDetail.Count > 0)
            {
                string detail = ToJson(wagerDetail);
                string GameId = wagerDetail.GetValueOrDefault("gameId")?.ToString();

                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                        RefTransId = wagerDetail.GetValueOrDefault("transactionId")?.ToString(),
                        TpUserName = wagerDetail.GetValueOrDefault("userReferenceId")?.ToString(),
                        UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("transactionDate", DateTime.MinValue),
                        UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("sessionEndTime", DateTime.MinValue),
                        Stake = (decimal)wagerDetail.GetValueOrDefault("stake") * tpCurrency.ExchangeRate,
                        WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("won") - (decimal)wagerDetail.GetValueOrDefault("stake")) * tpCurrency.ExchangeRate,
                        GameCode = GetGameNameOfMini(inputs, GameId),
                        GameName = GetGameName(GetGameNameOfMini(inputs, GameId)),
                    },
                    _ => throw new NotImplementedException()
                };
            }

            // slot
            #region GetPlayerToken
            string uuid = Guid.NewGuid().ToString();
            var sParam = new
            {
                Timestamp = DateTime.Now.ToString(_formatOfSlot),
                Seq = uuid,
                SiteCode = SiteCode,
                PlayerId = tpCustInfo.TpUserName,
            };
            var param = ToJson(sParam);
            var result = HttpPost($"{ApiUrl}GetPlayerToken", param, "application/json");

            if (!IsJSON(result))
            {
                throw new BetDetailNotFoundException();
            }

            var response = JsonConvert.DeserializeObject<GetPlayerTokenrep>(result);
            string PlayerToken = response.Token;
            #endregion

            var gamecode = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_AP_Unsettle_Get", new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report)
                .GetValueOrDefault("gamecode").ToString();
            string Params = $"Token={PlayerToken}&SiteCode={SiteCode}&GameCode={gamecode}&GameRoundId={GameRoundId}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = $"{GameUrl}GameDetail?{Params}",
                TpCurrency = tpCurrency,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            // mini game
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_AP_Mini_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            if (wagerDetail != null && wagerDetail.Count > 0)
            {
                var GameId = wagerDetail.GetValueOrDefault("gameId")?.ToString();

                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = GetGameNameOfMini(inputs, GameId),
                    Game = GetGameName(GetGameNameOfMini(inputs, GameId)),
                    RefNo = wagerDetail.GetValueOrDefault("transactionId")?.ToString(),
                    Title = tpGameType.TypeName,
                    MatchID = wagerDetail.GetValueOrDefault("sessionId")?.ToString(),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Odds = (decimal)wagerDetail.GetValueOrDefault("odd"),
                    Result = wagerDetail.GetValueOrDefault("result")?.ToString(),
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                };
            }

            // slot
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = TpBetRecordProvider.DbQuery("Service_BettransRng_AP_Unsettle_Get", new { YYYYMM, betRecordInfo.TransId }, DataBase.Main);
            var gameCode = detail.GetFirstStringValue("gamecode");
            var roundId = detail.GetFirstStringValue("gameroundid");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = roundId,
                Title = tpGameType.TypeName,
                MatchID = roundId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = detail.GetFirstStringValue("ip")
            };
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out string name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out string result) ? result : string.Empty;
        }

        private static string GetGameNameOfMini(Dictionary<string, string> inputs, string GameId)
        {
            var flip = inputs.ToDictionary(x => x.Value, x => x.Key);
            string GameName = flip.ContainsKey(GameId) && !string.IsNullOrEmpty(flip[GameId]) ? flip[GameId].Split("_")[1] : "";
            return GameName;
        }

        private string GetLoginOfMiniGame(TpCustInfo tpCustInfo, string TpLangId, APConfig config, string gameId, string SiteCode)
        {
            string GameUrl = config.GameUrl_MiniGame;
            string SecretKey = config.SecretKey_MiniGame;

            Dictionary<string, string> MiniGameId = new()
            {
                { "crash", config.MiniGame_Crash },
                { "cardracing", config.MiniGame_CardRacing },
            };

            var Param = new
            {
                gameId = MiniGameId[gameId.ToLower()],
                userReferenceId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                balance = 0,
                language = TpLangId,
                requestTime = DateTime.UtcNow.ToString(_formatOfMini),
            };

            var sParam = ToJson(Param);
            var payload = AesEncryptV2(sParam, SecretKey);
            var signature = HmacSha256(payload, SecretKey);
            return $"{GameUrl}/client/auth?payload={HttpUtility.UrlEncode(payload)}&signature={HttpUtility.UrlEncode(signature)}&platform={SiteCode}";
        }

        private static string HmacSha256(string json, string key)
        {
            using (HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(key)))
            {
                byte[] encryptValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(encryptValue);
            };
        }
    }
}
