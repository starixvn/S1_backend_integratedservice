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
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Joker
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly JokerConfig _siteConfig;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<JokerConfig>(TpEnum.Joker.ToString());
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Joker).ToDictionary(d => d.AppId, d => d.Name);
        }

        #region Model

        public class UsrReq
        {
            public string AppID { get; set; }
            public string Hash { get; set; }
            public long Timestamp { get; set; }
        }
        public class LoginReq
        {
            public string token { get; set; }
            public string appID { get; set; }
            public string gameCode { get; set; }
            public string language { get; set; }
            public bool mobile { get; set; }
            public string redirectUrl { get; set; }
        }

        public class DetailResult
        {
            public string Error { get; set; }
            public string Description { get; set; }
            public string Url { get; set; }
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
            var config = _siteConfig;
            var currencyData = _siteConfig.CurrencyData[tpCustInfo.TpCurrencyId];

            string gameUrl = _siteConfig.GameUrl;
            string appId = currencyData.AppID;
            string secret = currencyData.Secret;

            if (tpCustInfo.IsTest)
            {
                appId = currencyData.AppID_Test;
                secret = currencyData.Secret_Test;
            }

            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var token = ApplyToken(tpCustInfo);
            var sessionKeyArr = new string[] { token, gameCode };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = Convert.ToBase64String(bytes);

            var loginReq = new LoginReq
            {
                token = sessionKey,
                appID = appId,
                gameCode = gameCode,
                language = TpLangId,
                mobile = appType != AppType.Desktop,
                redirectUrl = homeUrl
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{gameUrl}?{sParam}"
            };
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                var config = _siteConfig;
                var currencyData = _siteConfig.CurrencyData[tpCustInfo.TpCurrencyId];

                string apiUrl = _siteConfig.ApiUrl;
                string appId = currencyData.AppID;
                string secret = currencyData.Secret;

                if (tpCustInfo.IsTest)
                {
                    appId = currencyData.AppID_Test;
                    secret = currencyData.Secret_Test;
                }

                var data = getBetDetail.TpGameTypeOption.Type == (int)TpGameType.Joker_RNG ?
                    TpBetRecordProvider.DbQueryFirst("Service_Bettrans_Joker_GameCode_Get", new { getBetDetail.TransId }, DataBase.Report) :
                    TpBetRecordProvider.DbQueryFirst("Service_BettransFish_Joker_Unsettle_Get", new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report);

                var url = $"{apiUrl}/game-history-url";
                var parameters = new Dictionary<string, string>()
                {
                    { "AppID", appId },
                    { "Username", tpCustInfo.TpUserName },
                    { "GameCode", data.GetValueOrDefault("gamecode")?.ToString()},
                    { "RoundID", getBetDetail.TransId},
                    { "Language","en"},
                    { "Timestamp",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()}
                };

                var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key.ToLower()}={kp.Value}"));
                parameters.Add("Hash", ToMD5($"{sParam}{secret}").ToLower());

                var json = HttpPost(url, ToJson(parameters), "application/json");
                var result = FromJson<DetailResult>(json);

                if (result.Error != "0")
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.Error} | {result.Description}");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.Url,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            catch (Exception e)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {e.Message}");
            }
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = tpGameType.Type == (int)TpGameType.Joker_RNG ?
                    TpBetRecordProvider.DbQuery("Service_Bettrans_Joker_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main) :
                    TpBetRecordProvider.DbQuery("Service_BettransFish_Joker_Unsettle_Get", new { YYYYMM, betRecordInfo.TransId }, DataBase.Main);

            var gameCode = detail.GetFirstStringValue("gamecode");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detail.GetFirstStringValue("roundid"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetFirstStringValue("roundid"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
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
    }
}
