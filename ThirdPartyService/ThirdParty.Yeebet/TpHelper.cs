using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Yeebet
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<string, string> _gameNameList = new()
        { { "4", "Baccarat" }, { "5", "Roulette" }, { "10", "Dragon Tiger" } };

        private readonly Dictionary<int, string> _PokerDic = new()
        {
            { 0, "NoCard" },
            { 1, "[D A]" },
            { 2, "[C A]" },
            { 3, "[H A]" },
            { 4, "[S A]" },
            { 5, "[D 2]" },
            { 6, "[C 2]" },
            { 7, "[H 2]" },
            { 8, "[S 2]" },
            { 9, "[D 3]" },
            { 10, "[C 3]" },
            { 11, "[H 3]" },
            { 12, "[S 3]" },
            { 13, "[D 4]" },
            { 14, "[C 4]" },
            { 15, "[H 4]" },
            { 16, "[S 4]" },
            { 17, "[D 5]" },
            { 18, "[C 5]" },
            { 19, "[H 5]" },
            { 20, "[S 5]" },
            { 21, "[D 6]" },
            { 22, "[C 6]" },
            { 23, "[H 6]" },
            { 24, "[S 6]" },
            { 25, "[D 7]" },
            { 26, "[C 7]" },
            { 27, "[H 7]" },
            { 28, "[S 7]" },
            { 29, "[D 8]" },
            { 30, "[C 8]" },
            { 31, "[H 8]" },
            { 32, "[S 8]" },
            { 33, "[D 9]" },
            { 34, "[C 9]" },
            { 35, "[H 9]" },
            { 36, "[S 9]" },
            { 37, "[D10]" },
            { 38, "[C10]" },
            { 39, "[H10]" },
            { 40, "[S10]" },
            { 41, "[D J]" },
            { 42, "[C J]" },
            { 43, "[H J]" },
            { 44, "[S J]" },
            { 45, "[D Q]" },
            { 46, "[C Q]" },
            { 47, "[H Q]" },
            { 48, "[S Q]" },
            { 49, "[D K]" },
            { 50, "[C K]" },
            { 51, "[H K]" },
            { 52, "[S K]" }
        };

        private readonly Dictionary<int, YeebetConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YeebetConfig>(TpEnum.Yeebet.ToString());
        }

        #region Model

        private class LoginResponse
        {
            /// <summary>
            /// 描述
            /// </summary>
            public string desc { get; set; }
            /// <summary>
            /// 结果信息(成功>=0，错误<0，小于零为异常)
            /// </summary>
            public int result { get; set; }
            /// <summary>
            /// 錯誤代碼
            /// </summary>
            public int errid { get; set; }
            /// <summary>
            /// 进入游戏链接
            /// </summary>
            public string openurl { get; set; }
        }

        #endregion

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new()
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var currencyData = config.CurrencyData[tpCustInfo.TpCurrencyId];

            string apiUrl = config.ApiUrl;
            string apiAppId = currencyData.ApiAppId;
            string secretKey = currencyData.ApiSecretKey;
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            apiUrl = $"{apiUrl}api/login";

            var loginRequest = new
            {
                appid = apiAppId,
                clienttype = (int)AppType.Desktop,
                currency = tpCustInfo.TpCurrencyId,
                iscreate = 1,
                language = int.Parse(TpLangId),
                nickname = tpCustInfo.TpUserName,
                username = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginRequest));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sign = BuildMD5(sParam + $"&key={secretKey}");

            var result = HttpPostClient(apiUrl, sParam + $"&sign={sign}", "application/x-www-form-urlencoded");

            if (!IsJSON(result))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result"
                };

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);

            if (json == null || json.result < 0)
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = json == null ? "-1" : json.errid.ToString(),
                    RawMessage = json == null ? "No Result" : json.desc,
                };

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = json.openurl
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
        }

        #region Transfer Methods

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new()
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        #endregion

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_Yeebet_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("serialnumber")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("username")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("createtimeConvert", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("settletimeConvert", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("commamount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winlost") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gameid")?.ToString(),
                    GameName = ConvertGameName(wagerDetail.GetValueOrDefault("gametype")?.ToString()),
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_Yeebet_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

            var gametype = detail.GetStringValue("gametype");

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("gameid"),
                    Game = ConvertGameName(gametype),
                    RefNo = detail.GetStringValue("id"),
                    Title = tpGameType.TypeName,
                    MatchID = $"{detail.GetStringValue("gameno")}_{detail.GetStringValue("gameroundid")}",
                    BetType = ConvertBetType(gametype, detail.GetStringValue("bettype")),
                    BetChoice = detail.GetStringValue("betpoint"),
                    Result = ConvertResult(gametype, detail.GetStringValue("gameresult")),
                    Odds = decimal.TryParse(detail.GetStringValue("betodds"), out var odds) ? odds : 0
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        #region Private Method

        private static string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }

        /// <summary>
        /// 參考tp文件 3.3 Game Type
        /// </summary>
        /// <param name="gameid"></param>
        /// <returns></returns>
        private string ConvertGameName(string gametype)
        {
            if (!_gameNameList.TryGetValue(gametype, out var name))
                return string.Empty;
            return name;
        }

        /// <summary>
        /// 參考tp文件 3.4 Game Play Description
        /// </summary>
        /// <param name="gameid"></param>
        /// <param name="bettype"></param>
        /// <returns></returns>
        private static string ConvertBetType(string gametype, string bettype)
        {
            if (gametype != "5")
                return "Default";

            switch (bettype)
            {
                case "1": return "Bet on Number";
                case "2": return "Bet on Big Small / Odd Even / Red Black";
                case "3": return "Bet Low,Middle,High";
                case "4": return "Bet Line1,Line2,Line3";
                case "5": return "Bet Split";
                case "6": return "Bet 3 Number";
                case "7": return "Bet 4 Number";
                case "8": return "Bet 6 Number";
                default: return "Default";
            }
        }

        /// <summary>
        /// 參考tp文件 3.5 Game Result Code
        /// </summary>
        /// <param name="gameid"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private string ConvertResult(string gametype, string result)
        {
            switch (gametype)
            {
                case "4":
                    {
                        var resultList = result?.Split(";")?.Select(Int32.Parse)?.ToList();
                        return $"PLAYER = {_PokerDic[resultList[0]]}_{_PokerDic[resultList[2]]}_{_PokerDic[resultList[4]]} " +
                            $"/ BANKER = {_PokerDic[resultList[1]]}_{_PokerDic[resultList[3]]}_{_PokerDic[resultList[5]]}";
                    }
                case "10":
                    {
                        var resultList = result?.Split(";")?.Select(Int32.Parse)?.ToList();
                        return $"Dragon = {_PokerDic[resultList[0]]} / Tiger = {_PokerDic[resultList[1]]}";
                    }
                case "5":
                default: return result;
            }
        }

        #endregion

    }
}