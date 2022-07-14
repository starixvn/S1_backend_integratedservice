using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.AP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        public TpHelper()
        {
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

            string LangId = TpLangId;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            bool FunPlay = fun == "1" ? true : false;
            string Params = $"Token={sessionKey}&SiteCode={SiteCode}&GameCode={gameId}&LangCode={LangId}&FunPlay={FunPlay}";

            string URL = $"{GameUrl}LaunchGame?{Params}";
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = URL;
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

            var gamecode = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_AP_Unsettle_Get", new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report)
                .GetValueOrDefault("gamecode").ToString();

            string PlayerToken = string.Empty;

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
            PlayerToken = response.Token;
            #endregion

            string Params = $"Token={PlayerToken}&SiteCode={SiteCode}&GameCode={gamecode}&GameRoundId={GameRoundId}";

            string URL = $"{GameUrl}GameDetail?{Params}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = URL,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = TpBetRecordProvider.DbQuery("Service_BettransRng_AP_Unsettle_Get",
                new { YYYYMM, betRecordInfo.TransId }, DataBase.Main);
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
    }
}
