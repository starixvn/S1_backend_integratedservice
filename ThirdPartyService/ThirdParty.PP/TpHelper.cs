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

namespace ThirdParty.PP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, PPConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.PP).ToDictionary(d => d.GameId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PPConfig>(TpEnum.PP.ToString());
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
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = config.ApiUrl;
            string GameUrl = config.GameUrl;
            string SecureLogin = config.SecureLogin;
            string SecureKey = config.SecureKey;
            if (tpCustInfo.MemberType == CustomerMemberTypeEnum.LiveStreaming)
            {
                ApiUrl = config.ApiUrl_Stream;
                SecureKey = config.SecureKey_Stream;
                SecureLogin = config.SecureLogin_Stream;
            }

            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";

            string Uri = "/v3/http/CasinoGameAPI/game/url/";
            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin", SecureLogin },
                { "token", ApplyToken(tpCustInfo) },
                { "symbol", gameId },
                { "technology", "H5"},
                { "platform", (appType == AppType.Desktop) ? "WEB" : "MOBILE" },
                { "language", TpLangId },
                { "lobbyUrl", lobbyUrl },
                { "cashierUrl", cashierUrl },
                { "playMode", (fun.Equals("1", StringComparison.OrdinalIgnoreCase)) ? "DEMO" : "REAL" },
                { "stylename", SecureLogin },
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{SecureKey}")}";
            var result = HttpPost($"{ApiUrl}{Uri}", sParam, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResult>(result);
            if (json.error == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.gameURL;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.description;
                oApiResult.RawErrorCode = json.error.ToString();
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = config.ApiUrl;
            string SecureLogin = config.SecureLogin;
            string SecureKey = config.SecureKey;
            if (tpCustInfo.MemberType == CustomerMemberTypeEnum.LiveStreaming)
            {
                ApiUrl = config.ApiUrl_Stream;
                SecureKey = config.SecureKey_Stream;
                SecureLogin = config.SecureLogin_Stream;
            }

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            if (Regex.IsMatch(getBetDetail.TransId, @"[a-z]"))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} is PromoWin, No bet detail.");
            }

            var gameId = TpBetRecordProvider.DbQueryFirst("Service_Bettrans_PP_GameId_Get",
                new { getBetDetail.TransId, GameType = getBetDetail.TpGameTypeOption.Type }, DataBase.Report).GetValueOrDefault("gameId")?.ToString();

            var url = $"{ApiUrl}/v3/http/HistoryAPI/OpenHistoryExtended/";
            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin", SecureLogin },
                { "playerId", tpCustInfo.TpUserName },
                { "gameId", gameId },
                { "roundId", getBetDetail.TransId},
                { "language", "en"}
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{SecureKey}")}";
            var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
            var result = FromJson<DetailResult>(json);
            if (result.error != "0")
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.description}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.url,
                TpCurrency = tpCurrency,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var isPromoWin = Regex.IsMatch(betRecordInfo.TransId, @"[a-z]");
            var detail = TpBetRecordProvider.DbQueryFirst("Service_Bettrans_PP_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type, isPromoWin }, DataBase.Main);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameId = detail.GetStringValue("gameId");
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = isPromoWin ? detail.GetStringValue("campaignId") : gameId,
                Game = isPromoWin ? tpGameType.TypeName : GetGameName(gameId),
                RefNo = isPromoWin ? detail.GetStringValue("reference") : detail.GetStringValue("roundId"),
                Title = isPromoWin ? "Promo Win" : detail.GetStringValue("roundDetails"),
                MatchID = isPromoWin ? string.Empty : detail.GetStringValue("roundId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = isPromoWin ? string.Empty : detail.GetStringValue("result"),
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        public override ApiResult AddPrizeToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string SecureLogin = config.SecureLogin;
            string SecureKey = config.SecureKey;

            var oApiResult = new ApiResult();
            var bonusCode = inputs.ContainsKey("bonusCode") && !string.IsNullOrEmpty(inputs["bonusCode"]) ? inputs["bonusCode"] : throw new Exception();
            //var requestId = inputs.ContainsKey("requestId")  && !string.IsNullOrEmpty(inputs["requestId"]) ? inputs["requestId"] : throw new Exception();
            var Uri = "/v3/http/FreeRoundsBonusAPI/v2/players/add";
            var requestId = Guid.NewGuid().ToString();

            var serializedPlayerList = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "playerList", new List<string> { tpCustInfo.TpUserName } }
            });

            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin", SecureLogin },
                { "bonusCode", bonusCode },
                { "requestId", requestId }
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{SecureKey}")}";

            var result = HttpPost($"{ApiUrl}{Uri}?{sParam}", serializedPlayerList, "application/json");

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<AddFreeSpinResult>(result);
            if (json.error == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.description;
                oApiResult.RawErrorCode = json.error.ToString();
            }

            return oApiResult;
        }

        public override ApiResult CreateBonusCode(int siteId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[siteId];
            string apiUrl = config.ApiUrl;
            string secureLogin = config.SecureLogin;
            string secureKey = config.SecureKey;
            var bonusCode = inputs["BonusCode"];
            var gameId = inputs["GameId"];
            var currency = inputs["Currency"];
            var round = inputs["Round"];
            var startDate = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            var expirationDate = DateTimeOffset.Now.AddDays(30).ToUnixTimeSeconds().ToString();
            var Uri = "/v3/http/FreeRoundsBonusAPI/v2/bonus/create";

            var oApiResult = new ApiResult();

            var betValues = GetBetScales(config, gameId, currency).gameList.FirstOrDefault(x => x.gameId == gameId);

            if (betValues == null)
            {
                oApiResult.RawMessage = "betValues No result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail; 
                return oApiResult;
            }

            var gameList = new GameList
            {
                gameId = gameId,
                betValues = betValues.betValues
            };

            var newGameList = new CreateFRB();
            newGameList.gameList.Add(gameList);

            var totalJson = JsonConvert.SerializeObject(newGameList);
 
            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin", secureLogin },
                { "bonusCode", bonusCode },
                { "startDate",startDate},
                { "expirationDate",expirationDate},
                { "rounds",round},
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{secureKey}")}";


            var result = HttpPost($"{apiUrl}{Uri}?{sParam}", totalJson, "application/json");

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "Create No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<CreateBonusCodeResult>(result);
            if (json.error == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.description;
                oApiResult.RawErrorCode = json.error.ToString();
            }

            return oApiResult;
        }

        public class DetailResult
        {
            public string error { get; set; }
            public string description { get; set; }
            public string url { get; set; }
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

        private CreateFRB GetBetScales(PPConfig config, string gameId, string currencyId)
        {
            string apiUrl = config.ApiUrl;
            string secureLogin = config.SecureLogin;
            string secureKey = config.SecureKey;
            var Uri = "/v3/http/CasinoGameAPI/getBetScales";

            var parametersTest = new Dictionary<string, string>()
            {
                { "secureLogin", secureLogin },
                { "gameIDs", gameId},
                { "currencies", currencyId }
            };

            var sParamTest = string.Join("&", parametersTest.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParamTest += $"&hash={ToMD5($"{sParamTest}{secureKey}")}";

            var result = HttpPost($"{apiUrl}{Uri}?{sParamTest}", "", "application/json");

            if (!IsJSON(result))
            {
                return null;
            }

            var json = JsonConvert.DeserializeObject<CreateBonusCodeResult>(result);
            if (json.error == 0)
            {
                var jObject = JObject.Parse(result);
                var newGameList = new CreateFRB();

                foreach (var game in jObject["gameList"])
                {
                    string Id = game["gameID"].ToString();
                    var gameItem = new GameList
                    {
                        gameId = Id,
                        betValues = new List<BetValues>()
                    };

                    // 遍歷每個貨幣
                    foreach (var betScale in game["betScaleList"])
                    {
                        string currency = betScale["currency"].ToString();
                        var totalBetScales = betScale["totalBetScales"].ToObject<decimal[]>();

                        // 找到最小值
                        decimal minTotalBetScale = totalBetScales.Min();

                        // 添加到遊戲物件
                        gameItem.betValues.Add(new BetValues { currency = currency, totalBet = minTotalBetScale });
                    }

                    // 添加到遊戲列表
                    newGameList.gameList.Add(gameItem);
                }
                return newGameList;
            }

            return null;
        }

    }    
}
