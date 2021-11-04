using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.EBet
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
            string currency = tpCustInfo.TpCurrencyId;
            string GameUrl = inputs.ContainsKey($"GameUrl_{currency}") && !string.IsNullOrEmpty(inputs[$"GameUrl_{currency}"]) ? inputs[$"GameUrl_{currency}"] : throw new Exception($"GameUrl_{currency} is empty");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            ApiResult oApiResult = new ApiResult();

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            string funParam = "";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                funParam = "&mode=trial";
            }
            string mobileParam = "";
            if (appType != AppType.Desktop)
            {
                mobileParam = "&orientation=1";
            }

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?username={tpCustInfo.TpUserName}&accessToken={HttpUtility.UrlEncode(key)}&language={TpLangId}{mobileParam}{funParam}";
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var currency = tpCustInfo.TpCurrencyId;
            var apiUrl = inputs.GetValueOrDefault($"ApiUrl_{currency}") ?? throw new Exception();
            var channelId = inputs.GetValueOrDefault($"ChannelId_{currency}") ?? throw new Exception();
            var privateKey = inputs.GetValueOrDefault($"PrivateKey_{currency}") ?? throw new Exception();
           
            var detail = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransLC_EBet_Detail_Get", new { getBetDetail.TransId });
            var roundCode = detail.GetValueOrDefault("roundCode").ToString();
            var username = detail.GetValueOrDefault("username").ToString();
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var signature = RSA($"{username}{timestamp}", privateKey);
            var lang = "en_us";
            var sParam = new
            {
                channelId,
                timestamp,
                signature,
                username,
                roundCode,
                currency,
                lang
            };

            var url = $"{apiUrl}/api/userBetTracer";
            var json = HttpPost(url, ToJson(sParam), "application/json");
            var result = FromJson<DetailResult>(json);
            if (result.status != 200)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = $"{result.path}&signature={RSA($"{channelId}{username}{roundCode}{lang}", privateKey)}",
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public class DetailResult
        {
            public int status { get; set; }
            public string path { get; set; }
        }

    }
}
