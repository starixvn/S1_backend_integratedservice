using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.PG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class DetailData
        {
            public string operator_session { get; set; }
        }
        public class DetailResponse
        {
            public DetailData data { get; set; }
        }

        private readonly IReadOnlyDictionary<string, string> GameNameList;

        public TpHelper()
        {
            GameNameList = TpBetRecordProvider.GetGameCasinoList(14).ToDictionary(d => d.AppId, d => d.Name);
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
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception("GameUrl");
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");
            string operator_token = inputs.ContainsKey("operator_token") && !string.IsNullOrEmpty(inputs["operator_token"]) ? inputs["operator_token"] : throw new Exception("operator_token");
            string secret_key = inputs.ContainsKey("secret_key") && !string.IsNullOrEmpty(inputs["secret_key"]) ? inputs["secret_key"] : throw new Exception("secret_key");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = string.Format(FunGameUrl, gameId, TpLangId, HttpUtility.UrlEncode(operator_token), HttpUtility.UrlEncode(lobbyUrl));
                return oApiResult;
            }

            string bet_type = "1";
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            //if (tpCustInfo.TpCurrencyId == "CNY")
            //{
            //    GameUrl = inputs.ContainsKey($"GameUrl_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"GameUrl_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"GameUrl_{tpCustInfo.TpCurrencyId}"] : throw new Exception($"GameUrl_{tpCustInfo.TpCurrencyId}");
            //}

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = string.Format(GameUrl, gameId, TpLangId, bet_type, HttpUtility.UrlEncode(operator_token), HttpUtility.UrlEncode(sessionKey), HttpUtility.UrlEncode(lobbyUrl));
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //var tpBetRecordDetail = TpBetRecordProvider.GetTpBetRecordDetail(getBetDetail);

            //if (tpBetRecordDetail == null)
            //{
            //    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            //}

            //var model = JsonConvert.DeserializeObject<DetailModel>(tpBetRecordDetail.Detail);
            //return new CasinoDetail(getBetDetail.TpGameTypeOption)
            //{
            //    TransId = getBetDetail.TransId,
            //    Detail = tpBetRecordDetail.Detail,
            //    TpCurrency = new TpCurrency()
            //    {
            //        ExchangeRate = tpCustInfo.ExchangeRate,
            //        TpCurrencyId = tpCustInfo.TpCurrencyId,
            //    },
            //    RefTransId = model?.parentBetId.ToString(),
            //    TpUserName = model?.playerName,
            //    UTCBetTime = DateTimeOffset.FromUnixTimeMilliseconds(model?.betTime ?? 0).DateTime,
            //    UTCSettledTime = DateTimeOffset.FromUnixTimeMilliseconds(model?.betEndTime ?? 0).DateTime,
            //    Stake = model.betAmount * tpCustInfo.ExchangeRate,
            //    WinlostAmount = (model.winAmount - model.betAmount) * tpCustInfo.ExchangeRate,
            //    GameCode = model.gameId.ToString(),
            //    GameName = GetGameName(model.gameId.ToString()),
            //};
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string BetDetailUrl = inputs.ContainsKey("BetDetailUrl") && !string.IsNullOrEmpty(inputs["BetDetailUrl"]) ? inputs["BetDetailUrl"] : throw new Exception("BetDetailUrl");
            string operator_token = inputs.ContainsKey("operator_token") && !string.IsNullOrEmpty(inputs["operator_token"]) ? inputs["operator_token"] : throw new Exception("operator_token");
            string secret_key = inputs.ContainsKey("secret_key") && !string.IsNullOrEmpty(inputs["secret_key"]) ? inputs["secret_key"] : throw new Exception("secret_key");
            string Uri = $"/Login/v1/LoginProxy";

            var detail = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransRng_PG_Detail_Get", new { getBetDetail.TransId });

            string TraceId = detail.GetValueOrDefault("TraceId")?.ToString();
            string ParentBetId = detail.GetValueOrDefault("ParentBetId").ToString();
            string BetId = detail.GetValueOrDefault("BetId").ToString();
            if (string.IsNullOrEmpty(TraceId))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }

            var BetDetail = new
            {
                operator_token = operator_token,
                secret_key = secret_key
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(BetDetail));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(
                $"{ApiUrl}{Uri}?trace_id={TraceId}",
                sParam,
                "application/x-www-form-urlencoded"
            );

            if (!IsJSON(result))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result}");
            }

            var json = JsonConvert.DeserializeObject<DetailResponse>(result);
            if (string.IsNullOrEmpty(json.data.operator_session))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail error.");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = $"{BetDetailUrl}?trace_id={TraceId}&t={json.data.operator_session}&psid={ParentBetId}&sid={BetId}&lang=en&type=operator",
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        private string GetGameName(string gameId)
        {
            if (!GameNameList.TryGetValue(gameId, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }
    }
}
