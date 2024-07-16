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
        private readonly APConfig _siteConfigCommon;
        Dictionary<string, string> _domainRule = new Dictionary<string, string>() {
            {"DEV_SLOT","https://sit." },
            {"PRD_SLOT","https://game." },
            {"PRD_MINI","https://mini." },
        };

        public TpHelper(IServiceProvider provider)
        {
            _gameListOfMini = new List<string> { "Crash", "CardRacing" };
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.AP).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<APConfig>(TpEnum.AP.ToString());
            _siteConfigCommon = provider.GetRequiredService<ConfigBLL>().GetConfigValue<APConfig>(TpEnum.AP.ToString());
        }

        #region Response

        public class GetPlayerTokenResp
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string Token { get; set; }
        }

        public class GetLaunchDomainResp
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string LaunchDomain { get; set; }
        }

        public class GetLaunchURLResp
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string LaunchURL { get; set; }
        }

        public class GetAddPrizeToPlayerResp
        {
            public string Timestamp { get; set; }
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorDescription { get; set; }
            public string CampaignId { get; set; }
            public List<string> PlayerList { get; set; }
        }

        #endregion

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
            string ApiUrl = Config.ApiUrl;
            string GameUrl = Config.GameUrl;
            string SiteCode = Config.SiteCode;

            string LangId = TpLangId;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            bool FunPlay = fun == "1";

            ApiResult oApiResult = new();

            if (_gameListOfMini.Contains(gameId))
            {
                var subGameUrl = "";
                // 取得最新的遊戲網址
                var domainResult = HttpGet($"{ApiUrl}GetLaunchDomain", "", "application/json");

                if (IsJSON(domainResult))
                {
                    var domainResponse = JsonConvert.DeserializeObject<GetLaunchDomainResp>(domainResult);
                    // DEV & PRD 網址不同，排除DEV使用替換網址
                    if (domainResponse.ErrorCode == 0 && !string.IsNullOrEmpty(domainResponse.LaunchDomain) && !domainResponse.LaunchDomain.Contains(_domainRule["DEV_SLOT"]))
                    {
                        subGameUrl = domainResponse.LaunchDomain;
                    }
                }

                // mini game
                if (FunPlay)
                {
                    DefTpCurrencyId DefaultTpCurrencyId = ServerInfoProvider.GetDefaultTpCurrencyIdByTpId(tpCustInfo.SiteId, (int)TpEnum.AP);
                    tpCustInfo.TpUserName = "";
                    tpCustInfo.TpCurrencyId = (DefaultTpCurrencyId != null) ? DefaultTpCurrencyId.TpCurrencyId : "";
                }
                string URL = GetLoginOfMiniGame(tpCustInfo, TpLangId, Config, gameId, SiteCode, subGameUrl);
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = URL;
            }
            else
            {
                // 取得進入遊戲網址
                var sParam = new
                {
                    Timestamp = DateTime.Now.ToString(_formatOfSlot),
                    Seq = Guid.NewGuid().ToString(),
                    SiteCode = SiteCode,
                    Token = sessionKey,
                    GameCode = gameId,
                    LangCode = LangId,
                    FunPlay = FunPlay,
                };
                string URL = "";
                var param = ToJson(sParam);
                var launchResult = HttpPost($"{ApiUrl}GetLaunchURL", param, "application/json");
                if (IsJSON(launchResult))
                {
                    var launchResponse = JsonConvert.DeserializeObject<GetLaunchURLResp>(launchResult);
                    // 有取到進入遊戲網址 替換原本遊戲網址
                    if (launchResponse.ErrorCode == 0 && !string.IsNullOrEmpty(launchResponse.LaunchURL))
                    {
                        URL = launchResponse.LaunchURL;
                    }
                }
                else
                {
                    // 取不到進入遊戲網址 使用原本組連結方式
                    string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&LangCode={LangId}&FunPlay={FunPlay}";
                    URL = $"{GameUrl}LaunchGame?{Params}";
                }

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
                        GameCode = GetGameNameOfMini(Config, GameId),
                        GameName = GetGameName(GetGameNameOfMini(Config, GameId)),
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

            var response = JsonConvert.DeserializeObject<GetPlayerTokenResp>(result);
            string PlayerToken = response.Token;
            #endregion

            var domainResult = HttpGet($"{ApiUrl}GetLaunchDomain", "", "application/json");
            if (IsJSON(domainResult))
            {
                var domainResponse = JsonConvert.DeserializeObject<GetLaunchDomainResp>(domainResult);
                // DEV & PRD 網址不同，排除DEV使用替換網址
                if (domainResponse.ErrorCode == 0 && !string.IsNullOrEmpty(domainResponse.LaunchDomain) && !domainResponse.LaunchDomain.Contains(_domainRule["DEV_SLOT"]))
                {
                    var DetailSiteCode = _siteConfigCommon.DetailSiteCode;
                    GameUrl = $"{domainResponse.LaunchDomain}/web/{DetailSiteCode}/";
                }
            }

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
                var Config = _siteConfigDic[tpCustInfo.SiteId];
                var GameId = wagerDetail.GetValueOrDefault("gameId")?.ToString();

                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = GetGameNameOfMini(Config, GameId),
                    Game = GetGameName(GetGameNameOfMini(Config, GameId)),
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

        public override ApiResult AddPrizeToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var Config = _siteConfigDic[tpCustInfo.SiteId];
            var ApiUrl = Config.ApiUrl;
            var SiteCode = Config.SiteCode;
            var campaignId = inputs.ContainsKey("CampaignId") && !string.IsNullOrEmpty(inputs["CampaignId"]) ? inputs["CampaignId"] : throw new Exception();
            var operatorUser = inputs.ContainsKey("operatorUser") && !string.IsNullOrEmpty(inputs["operatorUser"]) ? inputs["operatorUser"] : throw new Exception();
            var oApiResult = new ApiResult();

            var uuid = Guid.NewGuid().ToString();
            var sParam = new
            {
                Timestamp = DateTime.Now.ToString(_formatOfSlot),
                Seq = uuid,
                SiteCode = SiteCode,
                CampaignId = campaignId,
                PlayerList = new List<string> { tpCustInfo.TpUserName },
                Operator = operatorUser,
            };
            var param = ToJson(sParam);
            var result = HttpPost($"{ApiUrl}CPAddPlayer", param, "application/json");

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var response = JsonConvert.DeserializeObject<GetAddPrizeToPlayerResp>(result);
            if (response.ErrorCode == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.RawErrorCode = response.ErrorCode.ToString();
                oApiResult.RawMessage = string.Join(",", response.PlayerList);
                oApiResult.Message = "Success";
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = response.ErrorDescription;
                oApiResult.RawErrorCode = response.ErrorCode.ToString();
            }

            return oApiResult;
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

        private static string GetGameNameOfMini(APConfig config, string GameId)
        {
            var GameName = "";
            if (config.MiniGame_CardRacing == GameId)
            {
                GameName = "CardRacing";
            }
            else if (config.MiniGame_Crash == GameId)
            {
                GameName = "Crash";
            }
            return GameName;
        }

        private string GetLoginOfMiniGame(TpCustInfo tpCustInfo, string TpLangId, APConfig config, string gameId, string SiteCode, string SubGameUrl)
        {
            string GameUrl = config.GameUrl_MiniGame;
            string SecretKey = config.SecretKey_MiniGame;

            /*
             * 此次開發不含mini的部分，但您可透過GetLaunchDomain得知目前使用的主域名，再替換成mini。
             * 舉例 https://game.apchicken.online 轉換為https://mini.apchicken.online
             */
            if (!string.IsNullOrEmpty(SubGameUrl))
            {
                GameUrl = SubGameUrl.Replace(_domainRule["PRD_SLOT"], _domainRule["PRD_MINI"]);
            }

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
