using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.JILI
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
            string GameUrl = inputs.ContainsKey("GameUrl") && !String.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception($"GameUrl is empty");
            string HomeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string FunGameUrl = inputs.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(inputs["FunGameUrl"]) ? inputs["FunGameUrl"] : throw new Exception("FunGameUrl");
            string AgentId = inputs.ContainsKey("AgentId") && !String.IsNullOrEmpty(inputs["AgentId"]) ? inputs["AgentId"] : throw new Exception($"AgentId is empty");
            string AgentKey = inputs.ContainsKey("AgentKey") && !String.IsNullOrEmpty(inputs["AgentKey"]) ? inputs["AgentKey"] : throw new Exception($"AgentKey is empty");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            GameUrl = $"{GameUrl}/Login";

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}/{gameId}/{TpLangId}";
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameId };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var now = new DateTimeOffset(DateTime.UtcNow);
            var current = now.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyMMd");
            string g = ToMD5($"{current}{AgentId}{AgentKey}").ToLower();
            var param = new
            {
                Token = sessionKey,//HttpUtility.UrlEncode(sessionKey)
                GameId = gameId,
                Lang = TpLangId,
                AgentId = AgentId
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            string md5Param = ToMD5($"{sParam}{g}").ToLower();
            string k = $"{GetRandomString(6)}{md5Param}{GetRandomString(6)}";

            oApiResult.ErrorCode = ErrorCode.Success;
            //oApiResult.Message = $"{GameUrl}?Token={HttpUtility.UrlEncode(sessionKey)}&GameId={gameId}&Lang={TpLangId}&AgentId={AgentId}&HomeUrl={HomeUrl}&Key={k}";
            oApiResult.Message = $"{GameUrl}?{sParam}&HomeUrl={HomeUrl}&Key={k}";
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception();
            var agentId = inputs.GetValueOrDefault("AgentId") ?? throw new Exception();
            var agentKey = inputs.GetValueOrDefault("AgentKey") ?? throw new Exception();

            var url = $"{apiUrl}/GetGameDetailUrl";
            var current = new DateTimeOffset(DateTime.UtcNow).ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyMMd");
            var g = ToMD5($"{current}{agentId}{agentKey}").ToLower();

            var dic = new Dictionary<string, string>
            {
                { "WagersId",getBetDetail.TransId},
                { "AgentId",agentId},
            };
            var sParam = string.Join("&", dic.Select(kp => $"{kp.Key}={kp.Value}"));
            var key = $"{GetRandomString(6)}{ToMD5($"{sParam}{g}").ToLower()}{GetRandomString(6)}";
            sParam += $"&Key={key}&Lang=en-US";

            var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
            var result = FromJson<DetailResult>(json);
            if (result.ErrorCode != 0)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.ErrorCode} | {result.Message}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.Data.Url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public class DetailResult
        {
            public int ErrorCode { get; set; }

            public string Message { get; set; }

            public Data Data { get; set; }
        }

        public class Data
        {
            public string Url { get; set; }
        }

    }
}
