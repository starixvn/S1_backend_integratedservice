using System;
using System.Collections.Generic;
using System.Net.Http;
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
        #region Model

        public class DetailResult
        {
            public int status { get; set; }
            public string path { get; set; }
        }

        public class GetLaunchUrlResponse
        {
            public int status { get; set; }
            public string launchUrl { get; set; }
            public string apiVersion { get; set; }
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
            string domainUrl = string.Empty;
            string key = string.Empty;
            string funParam = string.Empty;
            string mobileParam = string.Empty;

            var apiUrl = inputs.ContainsKey($"ApiUrl_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrWhiteSpace(inputs[$"ApiUrl_{tpCustInfo.TpCurrencyId}"])
                       ? inputs[$"ApiUrl_{tpCustInfo.TpCurrencyId}"]
                       : throw new Exception($"ApiUrl_{tpCustInfo.TpCurrencyId} is empty");

            var channelId = inputs.ContainsKey($"ChannelId_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrWhiteSpace(inputs[$"ChannelId_{tpCustInfo.TpCurrencyId}"])
                          ? inputs[$"ChannelId_{tpCustInfo.TpCurrencyId}"]
                          : throw new Exception($"ChannelId_{tpCustInfo.TpCurrencyId} is empty");

            var privateKey = inputs.ContainsKey($"PrivateKey_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrWhiteSpace(inputs[$"ApiUrl_{tpCustInfo.TpCurrencyId}"])
                           ? inputs[$"PrivateKey_{tpCustInfo.TpCurrencyId}"]
                           : throw new Exception($"PrivateKey_{tpCustInfo.TpCurrencyId} is empty");

            domainUrl = GetUrlForLogin(tpCustInfo.TpCurrencyId, apiUrl, channelId, privateKey, out string launchUrl)
                      ? launchUrl
                      : string.Empty;

            if (string.IsNullOrWhiteSpace(domainUrl))
            {
                //launchUrl沒有拿到資料，則用db的GameUrl
                domainUrl = inputs.ContainsKey($"GameUrl_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrWhiteSpace(inputs[$"GameUrl_{tpCustInfo.TpCurrencyId}"])
                          ? inputs[$"GameUrl_{tpCustInfo.TpCurrencyId}"]
                          : throw new Exception($"GameUrl_{tpCustInfo.TpCurrencyId} is empty");
            }

            string fun = inputs.ContainsKey("fun") && !string.IsNullOrWhiteSpace(inputs["fun"])
                       ? inputs["fun"]
                       : "0";

            key = ApplyToken(tpCustInfo);

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
                funParam = "&mode=trial";

            if (appType != AppType.Desktop)
                mobileParam = "&orientation=1";

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{domainUrl}?"
                        + $"username={tpCustInfo.TpUserName}"
                        + $"&accessToken={HttpUtility.UrlEncode(key)}"
                        + $"&language={TpLangId}"
                        + $"{mobileParam}"
                        + $"{funParam}"
            };
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
            var content = new StringContent(ToJson(sParam), Encoding.UTF8, "application/json");
            using var client = new HttpClient();
            var json = client.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;
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

        #region Login Step

        private bool GetUrlForLogin(string currency, string apiUrl, string channelId, string privateKey, out string launchUrl)
        {
            launchUrl = string.Empty;
            try
            {
                var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                var signature = RSA($"{channelId}{timestamp}", privateKey);
                int china = 0;

                var param = new
                {
                    channelId,
                    timestamp,
                    signature,
                    currency,
                    china
                };

                var url = $"{apiUrl}/api/launchUrl";
                var result = HttpPost(url, ToJson(param), "application/json");

                if (IsJSON(result))
                {
                    var userResult = FromJson<GetLaunchUrlResponse>(result);
                    if (userResult.status == 200) //Success
                        launchUrl = userResult.launchUrl;
                    else
                        return false;
                }
                else
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion

    }
}
