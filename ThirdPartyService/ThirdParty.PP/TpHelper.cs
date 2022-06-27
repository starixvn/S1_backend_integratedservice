using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Extensions;

namespace ThirdParty.PP
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class UserReq
        {
            public string username { get; set; }
            public string password { get; set; }
            public string providerId { get; set; }
        }
        public class gameReq
        {
            public string gameID { get; set; }
            public string gameName { get; set; }
            public string gameTypeID { get; set; }
            public string typeDescription { get; set; }
            public string technology { get; set; }
            public string platform { get; set; }
            public bool demoGameAvailable { get; set; }
            public string aspectRatio { get; set; }
            public string technologyID { get; set; }
            public int gameIdNumeric { get; set; }
            public List<string> jurisdictions { get; set; }
        }
        public class LoginReq
        {
            public string token { get; set; } //由玩家的运营商生成的安全令牌
            public string symbol { get; set; } //游戏的唯一标识符，如 vs20bl、vs15ad 等
            public string technology { get; set; } // H5 或 F（用于 WEB 平台）或 H5（用于 MOBILE 平台）
            public string platform { get; set; } //WEB（用于桌面设备）或 MOBILE（用于移动设备）
            public string language { get; set; } //ISO 639 - 1 标准的玩家语言（如 en、fr、it）
            public string cashierUrl { get; set; } //用于打开运营商网站收银台页面的 URL；
            public string lobbyUrl { get; set; } //用于打开运营商网站大厅页面的 URL；
            public string operatorGameHistoryUrl { get; set; }  //用于在运营商侧打开游戏历史记录页面的 URL
            //public string secureLogin { get; set; }  //运营商在 Pragmatic Play 侧的唯一标识符。
        }


        public class LoginResult
        {
            public int error { get; set; }
            public string description { get; set; }
            public string gameURL { get; set; }
        }

        public class UserResult
        {
            public string userId { get; set; }
            public string currency { get; set; }
            public decimal cash { get; set; }
            public decimal bonus { get; set; }
            public int error { get; set; }
            public string description { get; set; }

            //"userId":"421", "currency":"USD", "cash": 99999.99, "bonus": 99.99, "error":0, "description":"Success"
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //throw new NotImplementedException();
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string GameUrl = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"]) ? inputs["GameUrl"] : throw new Exception();
            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string SecureLogin = inputs.ContainsKey("SecureLogin") && !string.IsNullOrEmpty(inputs["SecureLogin"]) ? inputs["SecureLogin"] : throw new Exception();
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string SecureKey = inputs.ContainsKey("SecureKey") && !string.IsNullOrEmpty(inputs["SecureKey"]) ? inputs["SecureKey"] : throw new Exception("SecureKey");
            string Uri = "/v3/http/CasinoGameAPI/game/url/";


            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin",SecureLogin },
                { "token",ApplyToken(tpCustInfo) },
                { "symbol",gameId },
                { "technology","H5"},
                { "platform",(appType == AppType.Desktop) ? "WEB" : "MOBILE" },
                { "language",TpLangId },
                { "lobbyUrl",lobbyUrl },
                { "cashierUrl",cashierUrl },
                { "playMode",(fun.Equals("1", StringComparison.OrdinalIgnoreCase)) ? "DEMO" : "REAL" },
                { "stylename",SecureLogin },
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{SecureKey}")}";
            var result = HttpPost($"{ApiUrl}{Uri}", sParam, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResult>(result);
            if (json.error == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.gameURL;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.description;
                oApiResult.RawErrorCode = json.error.ToString();
            }

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
            var apiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception();
            var secureLogin = inputs.GetValueOrDefault("SecureLogin") ?? throw new Exception();
            var secureKey = inputs.GetValueOrDefault("SecureKey") ?? throw new Exception();

            if (Regex.IsMatch(getBetDetail.TransId, @"[a-z]"))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} is PromoWin, No bet detail.");
            }

            var gameId = TpBetRecordProvider.ReportDbQueryFirst("Service_Bettrans_PP_GameId_Get",
                new { getBetDetail.TransId, GameType = getBetDetail.TpGameTypeOption.Type }).GetValueOrDefault("gameId")?.ToString();

            var url = $"{apiUrl}/v3/http/HistoryAPI/OpenHistoryExtended/";
            var parameters = new Dictionary<string, string>()
            {
                { "secureLogin",secureLogin },
                { "playerId",tpCustInfo.TpUserName },
                { "gameId",gameId },
                { "roundId",getBetDetail.TransId},
                { "language","en"}
            };

            var sParam = string.Join("&", parameters.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"));
            sParam += $"&hash={ToMD5($"{sParam}{secureKey}")}";
            var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
            var result = FromJson<DetailResult>(json);
            if (result.error != "0")
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.description}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.url,
                TpCurrency = tpCurrency,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var isPromoWin = Regex.IsMatch(betRecordInfo.TransId, @"[a-z]");
            var detail = TpBetRecordProvider.WagerDbQueryFirst("Service_Bettrans_PP_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type, isPromoWin });

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = isPromoWin ? detail.GetStringValue("campaignId") : detail.GetStringValue("gameId"),
                Game = string.Empty,
                RefNo = isPromoWin ? detail.GetStringValue("reference") : detail.GetStringValue("roundId"),
                Title = isPromoWin ? "Promo Win" : detail.GetStringValue("roundDetails"),
                MatchID = isPromoWin ? string.Empty : detail.GetStringValue("roundId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = isPromoWin ? string.Empty : detail.GetStringValue("result"),
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };          
        }

        public class DetailResult
        {
            public string error { get; set; }
            public string description { get; set; }
            public string url { get; set; }
        }

    }


}
