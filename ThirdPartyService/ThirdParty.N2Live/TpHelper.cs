using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.N2Live
{
    public class TpHelper : ThirdPartyHelperBase
    {

        public class LoginRequest
        {
            public LoginReq request { get; set; }
        }

        public class LoginReq
        {
            public string action { get; set; }
            public string id { get; set; }
            public string userid { get; set; }
            public int vendorid { get; set; }
            public string sessiontoken { get; set; }
            public string clientip { get; set; }
            public string timestamp { get; set; }

        }

        public class LoginResult
        {
            public string source { get; set; }
            public int status { get; set; }
            public LoginRes result { get; set; }

        }

        public class LoginRes
        {
            public string action { get; set; }
            public string id { get; set; }
            public string userid { get; set; }
            public string username { get; set; }
            public string acode { get; set; }
            public int currencyid { get; set; }
            public decimal balance { get; set; }
            public int vendorid { get; set; }
            public string merchantpasscode { get; set; }
            public string sessiontoken { get; set; }
            public string clientip { get; set; }

        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

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
            string MobileUrl = inputs.ContainsKey("MobileUrl") && !string.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception("MobileUrl");
            string MerchantCode = inputs.ContainsKey("MerchantCode") && !string.IsNullOrEmpty(inputs["MerchantCode"]) ? inputs["MerchantCode"] : throw new Exception("MerchantCode");
            //string errorUrl = inputs.ContainsKey("errorUrl") && !string.IsNullOrEmpty(inputs["errorUrl"]) ? inputs["errorUrl"] : "errorUrl";

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.SiteId.ToString(), tpCustInfo.TpCurrencyId };
            byte[] bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            string sessionKey = Convert.ToBase64String(bytes);

            string LoginUrl = GameUrl;
            if (appType != AppType.Desktop)
            {
                LoginUrl = MobileUrl;
            }

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            //oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}&redirectURL={errorUrl}";
            oApiResult.Message = $"{LoginUrl}/SingleLogin?merchantcode={MerchantCode}&lang={TpLangId}&userId={tpCustInfo.TpUserName}&uuId={HttpUtility.UrlEncode(sessionKey)}";
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_N2Live_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            var gameCode = wagerDetail.GetStringValue("gamecode");

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("betid")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("login")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("startdate", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("enddate", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("bet_amount") * tpCurrency.ExchangeRate,
                    WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("payout_amount") - (decimal)wagerDetail.GetValueOrDefault("bet_amount")) * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_N2Live_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            var gameCode = detail.GetStringValue("gamecode");

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = detail.GetStringValue("betid"),
                    Title = tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("result"),
                    DeviceType = GetDeviceType(detail.GetStringValue("channel")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private DeviceType GetDeviceType(string device)
        {
            switch (device)
            {
                case "iosphone":
                case "iostablet":
                    return DeviceType.iOS;
                case "androidphone":
                case "androidtablet":
                    return DeviceType.Android;
                case "iosmobileweb":
                case "androidmobileweb":
                    return DeviceType.Mobile;
                case "desktopweb":
                    return DeviceType.Desktop;
                default:
                    return DeviceType.None;
            }
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "50002" => "Roulette",
                "50003" => "Lucky Boost Roulette",
                "51002" => "RNG Roulette",
                "51003" => "RNG Lucky Boost Roulette",
                "52002" => "RNG Roulette PT *",
                "60001" => "Sicbo",
                "61001" => "RNG Sicbo",
                "62001" => "RNG Sicbo PT *",
                "90091" => "Baccarat Commission",
                "90092" => "Baccarat Non-Commission",
                "91091" => "RNG Baccarat Commission",
                "91092" => "RNG Baccarat Non-Commission",
                "100004" => "Ultimate Blackjack",
                "110001" => "RNG Traditional Blackjack PT*",
                "110002" => "RNG Free Bet Blackjack PT*",
                "110003" => "RNG Blackjack Switch PT*",
                "210001" => "RNG Casino Hold’em",
                "300001" => "Bull Bull",
                "310001" => "RNG Bull Bull",
                "400002" => "Free Pick San Gong Non-Commission",
                "400004" => "Fortune Koi Non-Commission",
                "500002" => "Andar Bahar Non-Commission",
                "990000" => "Slot Game",
                _ => gameCode,
            };
        }

    }
}
