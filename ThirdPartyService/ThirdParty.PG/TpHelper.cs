using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.PG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<string, string> GameNameList;
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
            var tpBetRecordDetail = TpBetRecordProvider.GetTpBetRecordDetail(getBetDetail);
            var model = JsonConvert.DeserializeObject<PGDetailModel>(tpBetRecordDetail.Detail);
            return new CasinoDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = tpBetRecordDetail.Detail,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
                RefTransId = model?.parentBetId.ToString(),
                TpUserName = model?.playerName,
                UTCBetTime = DateTimeOffset.FromUnixTimeMilliseconds(model?.betTime ?? 0).DateTime,
                UTCSettledTime = DateTimeOffset.FromUnixTimeMilliseconds(model?.betEndTime ?? 0).DateTime,
                Stake = model.betAmount * tpCustInfo.ExchangeRate,
                WinlostAmount = (model.winAmount - model.betAmount) * tpCustInfo.ExchangeRate,
                GameCode = model.gameId.ToString(),
                GameName = GetGameName(model.gameId.ToString()),
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
