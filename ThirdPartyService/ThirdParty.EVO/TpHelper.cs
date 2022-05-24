using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.EVO
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
            var tpId = (int)TpEnum.EVO;
            var gameType = (int)TpGameType.EVO_LiveCasino;
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
                        category = "top_games",// baccarat
                        //table = new
                        //{
                        //    id = "k2oswnib7jjaaznw"
                        //},
                        //playMode = "reward_games"
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
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWagerLC_EVO_Detail_Get", new { getBetDetail.TransId });
            if (wagerDetail != null)
            {
                //general data
                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                        RefTransId = wagerDetail.GetValueOrDefault("transactionId")?.ToString(),
                        TpUserName = wagerDetail.GetValueOrDefault("playerId")?.ToString(),
                        UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("placedOn", DateTime.MinValue),
                        UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("settledAt", DateTime.MinValue),
                        Stake = (decimal)wagerDetail.GetValueOrDefault("stake") * tpCurrency.ExchangeRate,
                        WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("payout") - (decimal)wagerDetail.GetValueOrDefault("stake")) * tpCurrency.ExchangeRate,
                        GameCode = wagerDetail.GetValueOrDefault("gameType")?.ToString(),
                        GameName = wagerDetail.GetValueOrDefault("gameType")?.ToString(),
                    },
                    _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        Detail = detail,
                        TpCurrency = tpCurrency,
                    }
                };
            }
            else
            {
                //promo_payout data
                wagerDetail = TpBetRecordProvider.ReportDbQueryFirst("Service_Reward_EVO_Detail_Get", new { _PromoTransactionId = getBetDetail.TransId });

                var detail = ToJson(wagerDetail);
                return getBetDetail.TpGameTypeOption.CategoryId switch
                {
                    TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
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
