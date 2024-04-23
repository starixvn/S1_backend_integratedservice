using System.Text;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.TFG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public TpHelper()
        {
        }
        public class GameURL
        {
            public string launch_url { get; set; }
        }
        public class Root
        {
            public int count { get; set; }
            public string next { get; set; }
            public string previous { get; set; }
            public List<TFGResult> results { get; set; }
        }
        public class TFGResult
        {
            public string id { get; set; }
            public decimal odds { get; set; }
            public string currency { get; set; }
            public decimal amount { get; set; }
            public string game_market_name { get; set; }
            public string market_option { get; set; }
            public string map_num { get; set; }
            public string bet_type_name { get; set; }
            public int event_id { get; set; }
            private DateTimeOffset _event_datetime { get; set; }
            public DateTimeOffset? event_datetime
            {
                get => _event_datetime;
                set
                {
                    if (value != null)
                        _event_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
                }
            }
            private DateTimeOffset? _date_created { get; set; }
            public DateTimeOffset? date_created
            {
                get => _date_created;
                set
                {
                    if (value != null)
                        _date_created = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
                }
            }
            private DateTimeOffset? _settlement_datetime { get; set; }
            public DateTimeOffset? settlement_datetime
            {
                get => _settlement_datetime;
                set
                {
                    if (value != null)
                        _settlement_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
                }
            }
            private DateTimeOffset? _modified_datetime { get; set; }
            public DateTimeOffset? modified_datetime
            {
                get => _modified_datetime;
                set
                {
                    if (value != null)
                        _modified_datetime = new DateTimeOffset(value.Value.ToUniversalTime().DateTime, TimeSpan.Zero);
                }
            }
            public string settlement_status { get; set; }
            public string result_status { get; set; }
            public decimal? earnings { get; set; }
            public bool is_combo { get; set; }
            public string member_code { get; set; }
            public bool is_unsettled { get; set; }
            public decimal? malay_odds { get; set; }
            public decimal? euro_odds { get; set; }
            public decimal member_odds { get; set; }
            public string member_odds_style { get; set; }
            public string request_source { get; set; }
            public List<Ticket> tickets { get; set; }
        }
        public class Ticket
        {
            public string id { get; set; }
            public string bet_selection { get; set; }
            public decimal odds { get; set; }
            public string game_type_name { get; set; }
            public int game_type_id { get; set; }
            public string game_market_name { get; set; }
            public string bet_type_name { get; set; }
            public string competition_name { get; set; }
            public int event_id { get; set; }
            public string event_name { get; set; }
            public DateTimeOffset? event_datetime { get; set; }
            public DateTimeOffset? date_created { get; set; }
            public DateTimeOffset? settlement_datetime { get; set; }
            public string settlement_status { get; set; }
            public string result_status { get; set; }
            public string handicap { get; set; }
            public bool is_unsettled { get; set; }
            public string ticket_type { get; set; }
            public decimal malay_odds { get; set; }
            public decimal euro_odds { get; set; }
            public decimal member_odds { get; set; }
            public string member_odds_style { get; set; }
            public string request_source { get; set; }
            public string result { get; set; }
            public string market_option { get; set; }
            public string map_num { get; set; }
        }
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string RetrieveUrl = inputs.ContainsKey("RetrieveUrl") && !string.IsNullOrEmpty(inputs["RetrieveUrl"]) ? inputs["RetrieveUrl"] : throw new Exception();
            string PublicToken = inputs.ContainsKey($"PublicToken_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"PublicToken_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"PublicToken_{tpCustInfo.TpCurrencyId}"] : throw new Exception();

            #region token
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            #endregion

            string gameUrl = $"{RetrieveUrl}/api/v4/launch/url/";
            var sParam = $"auth={PublicToken}";
            string json = HttpGet(gameUrl, sParam, "application/json");
            ApiResult oApiResult = new ApiResult();
            {
                if (IsJSON(json))
                {
                    var result = FromJson<GameURL>(json);
                    if (!string.IsNullOrWhiteSpace(result.launch_url))
                    {
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = $"{result.launch_url}?auth={PublicToken}&token={sessionKey}&lang={TpLangId}";
                    }
                    else
                    {
                        oApiResult.RawMessage = "No Result";
                        oApiResult.RawErrorCode = "1";
                        oApiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
            }
            return oApiResult;
        }
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string PrivateToken = inputs.ContainsKey($"PrivateToken_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"PrivateToken_{tpCustInfo.TpCurrencyId}"]) ? inputs[$"PrivateToken_{tpCustInfo.TpCurrencyId}"] : throw new Exception();

            string url = $"https://{ApiUrl}/api/v2/bet-transaction/";
            string getParams = $"id={getBetDetail.TransId}";
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Authorization", PrivateToken);
            string json = HttpGet(url, getParams, "application/json", header);
            var result = JsonConvert.DeserializeObject<Root>(json);
            var wager = result.results.FirstOrDefault();
            var winlostAmount = wager.result_status switch
            {
                "WIN" => wager.earnings - wager.amount,
                "LOSS" => wager.earnings,
                "DRAW" => wager.earnings - wager.amount,
                _ => 0,
            };
            return new SportsDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = json,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
                TpUserName = tpCustInfo.TpUserName,
                UTCBetTime = wager.date_created == null ? DateTime.MinValue : wager.date_created.Value.DateTime,
                UTCSettledTime = wager.settlement_datetime == null ? DateTime.MinValue : wager.settlement_datetime.Value.DateTime,
                Stake = wager.amount,
                WinlostAmount = winlostAmount.Value,
                Odds = wager.member_odds.ToString(),
                RefTransId = getBetDetail.TransId
            };
        }
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerTFG_Get", new { id = betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("event_id"),
                Game = tpGameType.TypeName,
                RefNo = string.Empty,
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = detail.GetStringValue("member_odds_style"),
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(detail.GetStringValue("request_source")),
                IP = string.Empty,
            };
        }
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "desktop-browser" => DeviceType.Desktop,
                "mobile-browser" => DeviceType.Mobile,
                "mobile-app" => DeviceType.Mobile,
                _ => DeviceType.None
            };
        }
    }
}