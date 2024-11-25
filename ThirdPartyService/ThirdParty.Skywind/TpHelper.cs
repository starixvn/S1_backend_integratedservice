using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Skywind
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, SkywindConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SkywindConfig>(TpEnum.Skywind.ToString());
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
            ApiResult<UserBalance> oApiResult = new()
            {
                ErrorCode = ErrorCode.Fail
            };
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
            string SecretKey = config.SecretKey;
            string Username = config.Username;
            string Password = config.Password;
            string LobbyUrl = config.StagingLobbies;

            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");

            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            #region SetCustGotoGame
            var tpGameType = (int)TpGameType.Skywind_RNG;
            if (gameId == "0")
            {
                tpGameType = (int)TpGameType.Skywind_LiveCasino;
            }
            else if (gameId == "sw_or" || gameId == "sw_fj" || gameId == "sw_cashdaha")
            {
                tpGameType = (int)TpGameType.Skywind_Fish;
            }
            TpCustInfoProvider.SetCustGotoGame(tpCustInfo.CustId, tpCustInfo.TpId, tpGameType);
            #endregion

            if (tpGameType == (int)TpGameType.Skywind_LiveCasino)
            {
                var lobbyUrl = $"{LobbyUrl}/#/ticket/{sessionKey}?language={TpLangId}";
                ApiResult oApiResult = new();
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = lobbyUrl;
                }
                return oApiResult;
            }
            else
            {
                var jwtToken = GetJwtToken(ApiUrl, Username, Password, SecretKey);
                Dictionary<string, string> header = new()
                {
                    { "X-ACCESS-TOKEN", jwtToken.accessToken }
                };

                var gameUrl = $"{ApiUrl}/players/{tpCustInfo.TpUserName}/games/{gameId}";
                var sParam = $"ticket={sessionKey}&language={TpLangId}";
                string json = HttpGet(gameUrl, sParam, "application/json", header);

                ApiResult oApiResult = new();
                {
                    if (IsJSON(json))
                    {
                        var result = FromJson<GameURL>(json);
                        if (!string.IsNullOrWhiteSpace(result.url))
                        {
                            oApiResult.ErrorCode = ErrorCode.Success;
                            oApiResult.Message = result.url;
                        }
                        else
                        {
                            oApiResult.RawMessage = "No Result";
                            oApiResult.RawErrorCode = "1";
                            oApiResult.ErrorCode = ErrorCode.Fail;
                        }
                    }
                    else
                    {
                        oApiResult.RawMessage = "No Result";
                        oApiResult.RawErrorCode = "1";
                        oApiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
                return oApiResult;
            }
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            Dictionary<string, string> itgGame = new()
            {
                { "itg_moneytreesofspring", "itg_moneytreesofspring" },
                { "itg_goldenroad", "itg_goldenroad" },
                { "itg_megatrail", "itg_megatrail" },
                { "itg_megamonkeys", "itg_megamonkeys" },
                { "itg_bigirishbucks", "itg_bigirishbucks" },
                { "itg_bigmoneyvegas", "itg_bigmoneyvegas" },
                { "itg_monkeyhero", "itg_monkeyhero" },
                { "itg_moneytreesofspring_9606", "itg_moneytreesofspring_9606" },
                { "itg_goldenroad_9599", "itg_goldenroad_9599" },
                { "itg_goldenroad_9401", "itg_goldenroad_9401" },
                { "itg_megatrail_9601", "itg_megatrail_9601" },
                { "itg_megatrail_9400", "itg_megatrail_9400" },
                { "itg_megamonkeys_9605", "itg_megamonkeys_9605" },
                { "itg_megamonkeys_9402", "itg_megamonkeys_9402" },
                { "itg_bigirishbucks_9599", "itg_bigirishbucks_9599" },
                { "itg_bigirishbucks_9404", "itg_bigirishbucks_9404" },
                { "itg_bigmoneyvegas_9600", "itg_bigmoneyvegas_9600" }
            };

            var config = _siteConfigDic[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string SecretKey = config.SecretKey;
            string Username = config.Username;
            string Password = config.Password;

            string[] sArray = Regex.Split(getBetDetail.TransId, "-", RegexOptions.IgnoreCase);
            var roundId = sArray[0];

            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_Skywind_Detail_Get", new { TransId = roundId }, DataBase.Wager);
            var GameID = detail.GetStringValue("gameCode");

            var jwtToken = GetJwtToken(ApiUrl, Username, Password, SecretKey);
            Dictionary<string, string> header = new()
            {
                { "X-ACCESS-TOKEN", jwtToken.accessToken }
            };

            if (itgGame.ContainsKey(GameID))
            {
                var detailUrl = $"{ApiUrl}/history/game";
                string getParams = $"roundId={roundId}";
                string json = HttpGet(detailUrl, getParams, "application/json", header);
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = json,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            else
            {
                var detailUrl = $"{ApiUrl}/history/game/{roundId}/image";
                string json = HttpGet(detailUrl, "", "application/json", header);
                var result = FromJson<DetailUrl>(json);

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.imageUrl,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_Skywind_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameCode"),
                Game = tpGameType.TypeName,
                RefNo = string.Empty,
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("roundId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(detail.GetStringValue("device")),
                IP = string.Empty,
            };
        }

        private JwtToken GetJwtToken(string apiUrl, string username, string password, string secretKey)
        {
            string url = $"{apiUrl}/login";
            var sParam = new
            {
                secretKey = secretKey,
                username = username,
                password = password
            };
            var param = ToJson(sParam);
            var r = HttpPost(url, param, "application/json");
            var result = JsonConvert.DeserializeObject<JwtToken>(r);
            return result;
        }

        private static DeviceType GetDeviceType(string device)
        {
            if (device == "web")
                return DeviceType.Desktop;
            else if (device == "mobile")
                return DeviceType.Mobile;
            else
                return DeviceType.None;
        }

        public class JwtToken
        {
            public string key { get; set; }
            public string username { get; set; }
            public string accessToken { get; set; }
        }

        public class GameURL
        {
            public string url { get; set; }
            public string token { get; set; }
        }

        public class DetailUrl
        {
            public string imageUrl { get; set; }
            public int ttl { get; set; }
        }
    }
}
