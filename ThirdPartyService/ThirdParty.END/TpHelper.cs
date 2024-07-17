using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.END
{
    public class TpHelper : ThirdPartyHelperBase
    {
        #region  Configs Setting

        private class TpConfig
        {
            public string ApiUrl { get; set; }

            public string GameUrl { get; set; }

            public string FunGameUrl { get; set; }

            public string NodeId { get; set; }

            public string EdemoId { get; set; }

            public string SecretKey { get; set; }

            public TpConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception("ApiUrl");
                GameUrl = inputs.GetValueOrDefault("GameUrl") ?? throw new Exception("GameUrl");
                FunGameUrl = inputs.GetValueOrDefault("FunGameUrl") ?? throw new Exception("FunGameUrl");
                NodeId = inputs.GetValueOrDefault("NodeId") ?? throw new Exception("NodeId");
                EdemoId = inputs.GetValueOrDefault("EdemoId") ?? throw new Exception("EdemoId");
                SecretKey = inputs.GetValueOrDefault("SecretKey") ?? throw new Exception("SecretKey");
            }
        }

        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper()
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Endorphina).ToDictionary(d => d.AppId, d => d.Name);
        }

        #endregion

        #region  Request

        private class LoginReq
        {
            public string exit { get; set; } = "";
            public string nodeId { get; set; } = "";
            public string profile { get; set; } = "";
            public string token { get; set; } = "";
            public string sign { get; set; } = "";
        }

        #endregion

        #region  Response

        private class GamesResponse
        {
            public string id { get; set; } = "";
            public string description { get; set; } = "";
            public List<string>? tags { get; set; }
            public List<string>? bonuses { get; set; }
            public List<int>? bets { get; set; }
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
            var tpConfig = new TpConfig(inputs);

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var YYYYMM = getBetDetail.SettledTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Endorphina_Detail_Get", new { YYYYMM, TransId = getBetDetail.TransId }, DataBase.Main);
            if (!detailList.Any() || detailList == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }

            var gameId = detailList.GetFirstStringValue("gameId");
            var token = detailList.GetFirstStringValue("token");

            // However, we don't keep replays in stage, so it can only be tested later in production already.
            var url = $"{tpConfig.ApiUrl}/replay-starter/replay?session={token}&round={gameId}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = url,
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
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Endorphina_Detail_Get", new { YYYYMM, TransId = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detailList.GetFirstStringValue("game");
            var gameId = detailList.GetFirstStringValue("gameId");
            var token = detailList.GetFirstStringValue("token");

            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = token,
                    Title = tpGameType.TypeName,
                    MatchID = gameId,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var tpConfig = new TpConfig(inputs);
            string gameId = inputs.GetValueOrDefault("gameId") ?? throw new Exception("gameId");
            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            string homeUrl = inputs.GetValueOrDefault("homeUrl") ?? "";
            string token = inputs.GetValueOrDefault("Token") ?? throw new Exception("Token");
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");

            ApiResult oApiResult = new ApiResult();
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                // Important: value of the returnURL should be urlencoded twice!
                var returnURL = HttpUtility.UrlEncode(HttpUtility.UrlEncode(homeUrl));
                var edomoUrl = $"{tpConfig.FunGameUrl}/api/link/accountId/{tpConfig.EdemoId}/hash/{ToMD5(gameId)}/returnURL/{returnURL}";
                var funUrl = HttpGet(edomoUrl, "", "");

                if (string.IsNullOrEmpty(funUrl) || !funUrl.Contains("/session/open/sid/"))
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    return oApiResult;
                }

                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = funUrl;
                return oApiResult;
            }

            tpCustInfo.GameId = inputs["gameId"];
            var key = ApplyToken(tpCustInfo);

            var loginReq = new LoginReq
            {
                exit = homeUrl,
                nodeId = tpConfig.NodeId,
                profile = "nofullscreen.xml",
                token = key.ToLower()
            };

            var dicP = FromJson<Dictionary<string, dynamic>>(ToJson(loginReq));
            var sParam = string.Join("", dicP.Where(x => x.Value != null).OrderBy(kp => kp.Key).Select(kp => kp.Value).ToArray());
            var p = $"{sParam}{tpConfig.SecretKey}";
            loginReq.sign = EncryptSha1(p).ToLower();
            var queryParam = $"exit={loginReq.exit}&nodeId={loginReq.nodeId}&profile={loginReq.profile}&token={loginReq.token}&sign={loginReq.sign}";

            oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{tpConfig.GameUrl}?{queryParam}"
            };

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult AddPrizeToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override ApiResult CreateBonusCode(int siteId, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private ApiResult<List<GamesResponse>> GetGameList(string TpCurrencyId, AppType appType, Dictionary<string, string> inputs)
        {
            var tpConfig = new TpConfig(inputs);
            var url = $"{tpConfig.ApiUrl}/api/seamless/rest/v1/games";
            var gameListReq = new
            {
                currency = TpCurrencyId,
                nodeId = tpConfig.NodeId,
            };
            var dicP = FromJson<Dictionary<string, dynamic>>(ToJson(gameListReq));
            var sParam = string.Join("", dicP.Where(x => x.Value != null).OrderBy(kp => kp.Key).Select(kp => kp.Value).ToArray());
            var p = $"{sParam}{tpConfig.SecretKey}";
            var sign = EncryptSha1(p).ToLower();

            var queryParam = $"currency={gameListReq.currency}&nodeId={gameListReq.nodeId}&sign={sign}";
            var json = HttpGet(url, queryParam, "application/x-www-form-urlencoded");

            var oApiResult = new ApiResult<List<GamesResponse>>
            {
                Data = new List<GamesResponse>()
            };

            if (IsJSON(json))
            {
                var data = JsonConvert.DeserializeObject<List<GamesResponse>>(json);

                if (data != null && data.Any())
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data = data;
                }
            }

            return oApiResult;
        }

        private string EncryptSha1(string str)
        {
            using SHA1 sha1Hash = System.Security.Cryptography.SHA1.Create();
            byte[] sourceBytes = Encoding.UTF8.GetBytes(str);
            byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
            string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            return hash;
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
