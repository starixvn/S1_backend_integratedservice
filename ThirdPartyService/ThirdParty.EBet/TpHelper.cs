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

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.WagerDbQueryFirst("Service_BettransLC_EBet_Detail_Get", new { betRecordInfo.TransId });
            var ebet = JsonConvert.DeserializeObject<DetailEbet>(detail.GetStringValue("detail"));
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = ebet.tableType.ToString(),
                    Game = GetGameName(ebet.tableType.ToString()),
                    RefNo = detail.GetStringValue("seqNo"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("roundCode"),
                    BetType = ebet.betList?.First()?.betType.ToString() ?? string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("results"),
                    DeviceType = GetDeviceType(detail.GetStringValue("platform")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "3" => DeviceType.Desktop,
                "1" => DeviceType.iOS,
                "0" => DeviceType.Android,
                "2" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "1" => "Baccarat",
                "2" => "Dragon Tiger",
                "3" => "Roulette",
                "4" => "Sic Bo(Dice)",
                "5" => "Slot",
                "8" => "BullBull",
                "23" => "Fortune Wheel",
                "24" => "Black Jack",
                "25" => "Live Black Jack",
                _ => gameCode,
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

                if (!IsJSON(result))
                    return false;

                var userResult = FromJson<GetLaunchUrlResponse>(result);

                if (userResult.status != 200) //If Not Success,This Step Fail.
                    return false;

                launchUrl = userResult.launchUrl;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion


        public class DetailEbet
        {
            public string tableCode { get; set; }
            public int tableType { get; set; }
            public string roundCode { get; set; }   
            public IList<BetListEbet> betList { get; set; }           
        }

        public class BetListEbet
        {
            public long betType { get; set; }
        }
    }
}
