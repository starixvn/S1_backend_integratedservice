using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.RedTiger
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<string, string> countryList = new Dictionary<string, string>() {
            {"MYR","MY"},
            {"CNY","CN"},
            {"IDR","ID"},
            {"THB","TH"},
            {"VND","VN"},
            {"INR","HT"},
        };

        public class LoginResponse
        {
            public string entry { get; set; }
            public string entryEmbedded { get; set; }
        }

        public class DetailResponse
        {
            public Result result { get; set; }
        }

        public class Result
        {
            public string link { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}";
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string Ua2Token = inputs.ContainsKey("Ua2Token") && !string.IsNullOrEmpty(inputs["Ua2Token"]) ? inputs["Ua2Token"] : throw new Exception("Ua2Token");
            string CasinoKey = inputs.ContainsKey("CasinoKey") && !string.IsNullOrEmpty(inputs["CasinoKey"]) ? inputs["CasinoKey"] : throw new Exception("CasinoKey");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string GameUrl = ApiUrl;
            ApiUrl = $"{ApiUrl}/ua/v1/{CasinoKey}/{Ua2Token}";

            bool mobile = appType != AppType.Desktop;
            var siteName = ServerInfoProvider.getSiteName(tpCustInfo.SiteId);

            ApiResult oApiResult = new ApiResult();

            if (string.IsNullOrEmpty(siteName))
            {
                oApiResult.RawMessage = "Site Error";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var tpId = (int)TpEnum.RedTiger;
            var gameType = (int)TpGameType.RedTiger_RNG;
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName, gameType.ToString(), tpId.ToString() };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            var Login = new
            {
                uuid = Guid.NewGuid().ToString(),
                player = new
                {
                    id = tpCustInfo.TpUserName,
                    update = true,
                    country = countryList[tpCustInfo.TpCurrencyId],
                    language = TpLangId,
                    currency = tpCustInfo.TpCurrencyId,
                    session = new
                    {
                        id = sessionKey,
                        ip = inputs["ClientIP"]
                    },
                    group = new
                    {
                        id = siteName.ToString(),
                        action = "assign"
                    }
                },
                config = new
                {
                    brand = new
                    {
                        id = siteName.ToString(),
                        skin = "1"
                    },
                    game = new
                    {
                        category = "slots",
                        //@interface = "view1",
                        table = new
                        {
                            id = gameId
                        },
                        //playMode = fun == "1" ? "play_for_fun" : "real_money"
                    },
                    channel = new
                    {
                        wrapped = mobile,
                        mobile = mobile
                    },
                    urls = new
                    {
                        lobby = homeUrl
                    },
                }
            };

            var result = HttpPost(
                 ApiUrl,
                 JsonConvert.SerializeObject(Login),
                 "application/json"
             );

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}{json.entry}";

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string GameProvider = inputs.ContainsKey("GameProvider") && !String.IsNullOrEmpty(inputs["GameProvider"]) ? inputs["GameProvider"] : throw new Exception($"GameProvider is empty");
            string gameHistoryApiToken = inputs.ContainsKey("gameHistoryApiToken") && !string.IsNullOrEmpty(inputs["gameHistoryApiToken"]) ? inputs["gameHistoryApiToken"] : throw new Exception("gameHistoryApiToken");
            string CasinoKey = inputs.ContainsKey("CasinoKey") && !string.IsNullOrEmpty(inputs["CasinoKey"]) ? inputs["CasinoKey"] : throw new Exception("CasinoKey");
            var WagerUrl = $"{ApiUrl}/api/gamehistory/v1/casino/games/stream";

            string word = "://";
            string[] cutHost = ApiUrl.Split(word);
            string BetDetailUrl = $"{cutHost[0]}{word}{CasinoKey}:{gameHistoryApiToken}@{cutHost[1]}";

            var slotData = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransRng_RedTiger_Detail_Get", new { TransId = getBetDetail.TransId });

            if (slotData != null)
            {
                //General Data
                var BetDetail = new
                {
                    startDate = slotData.GetValueOrDefault("CreateTime")?.ToString(),
                    gameProvider = GameProvider,
                    transactionId = getBetDetail.TransId
                };

                var dicP = FromJson<Dictionary<string, string>>(ToJson(BetDetail));
                var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes($"{CasinoKey}:{gameHistoryApiToken}")));

                var result = HttpGet(
                    WagerUrl,
                    sParam,
                    "text/plain",
                    header
                );

                if (!IsJSON(result))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result}");
                }

                var json = JsonConvert.DeserializeObject<DetailResponse>(result);
                if (json.result == null || string.IsNullOrEmpty(json.result.link))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail is null.");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = $"{BetDetailUrl}{json.result.link}",
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            else
            {
                //Reward Data

                var tpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                };

                var wagerDetail = TpBetRecordProvider.ReportDbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = getBetDetail.TransId });

                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                        RefTransId = wagerDetail.GetValueOrDefault("promotionTransactionId")?.ToString(),
                        TpUserName = wagerDetail.GetValueOrDefault("userId")?.ToString(),
                        UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("CreateTime", DateTime.MinValue),
                        UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("UpdateTime", DateTime.MinValue),
                        Stake = 0,
                        WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("payout"),
                        GameCode = wagerDetail.GetValueOrDefault("gameId")?.ToString(),
                        GameName = wagerDetail.GetValueOrDefault("gameType")?.ToString()
                    },
                    _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                    }
                };
            }

        }
    }
}
