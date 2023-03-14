using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ThirdPartyCore;
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
        private readonly List<string> _gameListOfMini;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        public TpHelper()
        {
            _gameListOfMini = new List<string> { "Crash", "CardRacing" };
            _gameNameList = TpBetRecordProvider.GetGameCasinoList(39).ToDictionary(d => d.AppId, d => d.Name);
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
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            string LangId = TpLangId;
            bool FunPlay = fun == "1" ? true : false;

            ApiResult oApiResult = new ApiResult();
            if (_gameListOfMini.Contains(gameId))
            {
                // mini game
                if (FunPlay)
                {
                    tpCustInfo.TpUserName = "";
                    tpCustInfo.TpCurrencyId = (tpCustInfo.SiteId == 1) ? "IDR" : "VND";
                }
                string URL = GetLoginOfMiniGame(tpCustInfo, TpLangId, inputs, gameId, SiteCode);
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
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string SiteCode = inputs.ContainsKey("SiteCode") && !string.IsNullOrEmpty(inputs["SiteCode"]) ? inputs["SiteCode"] : throw new Exception();
            string GameRoundId = getBetDetail.TransId;

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            // mini game
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerRng_AP_Mini_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            if (wagerDetail != null && wagerDetail.Count > 0)
            {
                var detail = ToJson(wagerDetail);
                var GameId = wagerDetail.GetValueOrDefault("gameId")?.ToString();

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
            var gamecode = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_AP_Unsettle_Get", new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report)
                .GetValueOrDefault("gamecode").ToString();

            #region GetPlayerToken
            string uuid = Guid.NewGuid().ToString();
            string Timestamp = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
            var sParam = new
            {
                Timestamp = Timestamp,
                Seq = uuid,
                SiteCode = SiteCode,
                PlayerId = tpCustInfo.TpUserName,
            };
            var param = ToJson(sParam);
            var result = HttpPost($"{ApiUrl}/GetPlayerToken", param, "application/json");

            if (!IsJSON(result))
            {
                throw new BetDetailNotFoundException();
            }

            var response = JsonConvert.DeserializeObject<GetPlayerTokenrep>(result);
            string PlayerToken = response.Token;
            #endregion

            string Params = $"Token={PlayerToken}&SiteCode={SiteCode}&GameCode={gamecode}&GameRoundId={GameRoundId}";

            string URL = $"{GameUrl}GameDetail?{Params}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = URL,
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
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        private string GetGameNameOfMini(Dictionary<string, string> inputs, string GameId)
        {
            var flip = inputs.ToDictionary(x => x.Value, x => x.Key);
            var GameName = flip.ContainsKey(GameId) && !string.IsNullOrEmpty(flip[GameId]) ? flip[GameId].Split("_")[1] : "";
            return GameName;
        }

        private string GetLoginOfMiniGame(TpCustInfo tpCustInfo, string TpLangId, Dictionary<string, string> inputs, string gameId, string SiteCode)
        {
            string GameUrl = inputs.ContainsKey("GameUrl_MiniGame") && !string.IsNullOrEmpty(inputs["GameUrl_MiniGame"]) ? inputs["GameUrl_MiniGame"] : throw new Exception("GameUrl_MiniGame");
            string SecretKey = inputs.ContainsKey("SecretKey_MiniGame") && !string.IsNullOrEmpty(inputs["SecretKey_MiniGame"]) ? inputs["SecretKey_MiniGame"] : throw new Exception("SecretKey_MiniGame");
            string MiniGameId = inputs.ContainsKey($"MiniGame_{gameId}") && !string.IsNullOrEmpty(inputs[$"MiniGame_{gameId}"]) ? inputs[$"MiniGame_{gameId}"] : throw new Exception($"MiniGame_{gameId}");
            var Param = new
            {
                gameId = MiniGameId,
                userReferenceId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                balance = 0,
                language = TpLangId,
                requestTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            };
            var sParam = ToJson(Param);
            var payload = AesEncryptV2(sParam, SecretKey);
            var signature = HmacSha256(payload, SecretKey);
            return $"{GameUrl}/client/auth?payload={HttpUtility.UrlEncode(payload)}&signature={HttpUtility.UrlEncode(signature)}&platform={SiteCode}";
        }

        private string HmacSha256(string json, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var encryptValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToBase64String(encryptValue);
            };
        }
    }
}
