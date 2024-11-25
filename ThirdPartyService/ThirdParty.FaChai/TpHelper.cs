using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdParty.FaChai.Models;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.FaChai
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly TemporaryLogSentryService temporaryLogSentryService;
        private readonly Dictionary<int, FaChaiConfig> _siteConfigDic;
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private const string _formatOfTime = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";
        private const int _utc = -4;
        private const string _testCurrency = "UUS";

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<FaChaiConfig>(TpEnum.FaChai.ToString());
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.FaChai).ToDictionary(d => d.GameId, d => d.Name);
            temporaryLogSentryService = provider.GetRequiredService<TemporaryLogSentryService>();
        }

        public class CreateMemberRequest
        {
            public string MemberAccount { get; set; }
        }

        public class FCResponse
        {
            public string Result { get; set; }
        }

        public class LoginRequest
        {
            public string MemberAccount { get; set; }
            public int GameID { get; set; }
            public int LanguageID { get; set; }
        }

        public class LoginResponse : FCResponse
        {
            public string Url { get; set; }
        }

        public class GetBetDetailRequest
        {
            public string MemberAccount { get; set; }
            public string RecordID { get; set; }
        }

        public class GetBetDetailResponse : FCResponse
        {
            public string Url { get; set; }
        }

        public class GetDemoUrlRequest
        {
            public int GameID { get; set; }
            public int LanguageID { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new()
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        //public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        //{
        //    var siteInputs = _siteConfigDic[tpCustInfo.SiteId];
        //    var request = new GetBetDetailRequest()
        //    {
        //        MemberAccount = tpCustInfo.TpUserName,
        //        RecordID = getBetDetail.TransId
        //    };

        //    var key = siteInputs.OperatorKey];
        //    var para = JsonConvert.SerializeObject(request);
        //    var Params = AesEncrypt(para, key);
        //    var Sign = ToMD5(para).ToLower();

        //    var dicP = new Dictionary<string, string>();
        //    dicP.Add("AgentCode", siteInputs["AgentCode"]);
        //    dicP.Add("Currency", tpCustInfo.TpCurrencyId);
        //    dicP.Add("Params", Params);
        //    dicP.Add("Sign", Sign);

        //    var result = HttpPostWithX509(
        //        $"{siteInputs["ApiUrl"]}/GetPlayerReport",
        //        dicP,
        //        "application/x-www-form-urlencoded"
        //    );

        //    var tpData = JsonConvert.DeserializeObject<GetBetDetailResponse>(result);

        //    return new BaseDetail(getBetDetail.TpGameTypeOption)
        //    {
        //        TransId = getBetDetail.TransId,
        //        DetailUrl = tpData.Url,
        //        TpCurrency = new TpCurrency()
        //        {
        //            ExchangeRate = tpCustInfo.ExchangeRate,
        //            TpCurrencyId = tpCustInfo.TpCurrencyId,
        //        },
        //    };
        //}

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var convert = new DateTimeOffset(getBetDetail.SettledTime);
            var time = convert.ToOffset(new TimeSpan(_utc, 0, 0)).ToString("yyyyMMdd");
            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    config.GameType,
                    OperatorCustId = tpCustInfo.TpUserName,
                    TicketNo = getBetDetail.TransId,
                    Language = "en-US",
                    AgentInfo = new
                    {
                        AgentKey = tpCustInfo.TpCurrencyId
                    },
                    OtherBetInfo = new
                    {
                        Platform = "D",
                        EventDate = time
                    }
                }
            };

            var ApiUrl = $"{config.ApiUrl}/api/CommonWallet/GetBetHistory";
            var postdata = JsonConvert.SerializeObject(input, Formatting.None);
            var header = new Dictionary<string, string>()
            {
                { "operator-pwd", config.OperatorPwd},
                { "operator-sign", Encrypt(postdata, config)},
            };

            var result = HttpPost(ApiUrl, postdata, "application/json", header);
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<GetBetHistoryResp>(result);
                if (json.ErrorCode == 0)
                {
                    return new BaseDetail(getBetDetail.TpGameTypeOption)
                    {
                        TransId = getBetDetail.TransId,
                        DetailUrl = json.Payload.ReturnUrl,
                        TpCurrency = tpCurrency,
                    };
                }
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                TpCurrency = tpCurrency,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_FaChai_TicketNo_Get", new { YYYYMM, TicketNo = betRecordInfo.TransId }, DataBase.Main);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            var GameId = detail.GetStringValue("GameId");
            var GameRoundId = detail.GetStringValue("GameRoundId");

            // No need MatchId
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = GameId,
                Game = GetGameName(GameId),
                RefNo = GameRoundId,
                Title = tpGameType.TypeName,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
                MatchID = string.Empty
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var ApiUrl = $"{config.ApiUrl}/api/CommonWallet/LaunchGame";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var key = ApplyToken(tpCustInfo);

            var input = new
            {
                Timestamp = GetCurrentTimeFormat(),
                Seq = Guid.NewGuid().ToString(),
                Payload = new
                {
                    ProductInfo = new
                    {
                        config.GameType,
                        GameId = gameId,
                        AppType = (appType == AppType.Desktop) ? "D" : "M",
                    },
                    CustInfo = new
                    {
                        OperatorCustomerId = tpCustInfo.TpUserName,
                        OperatorCustomerName = tpCustInfo.TpUserName,
                        Language = TpLangId,
                        Platform = (appType == AppType.Desktop) ? "D" : "M",
                        CurrencyISO = tpCustInfo.IsTest ? _testCurrency : tpCustInfo.TpCurrencyId,
                        IpAddress = inputs["ClientIP"],
                        WalletSideToken = key
                    },
                    HomeUrl = homeUrl,
                }
            };
            var postdata = JsonConvert.SerializeObject(input, Formatting.None);
            var header = new Dictionary<string, string>()
            {
                { "operator-pwd", config.OperatorPwd},
                { "operator-sign", Encrypt(postdata, config)},
            };

            var result = HttpPost(ApiUrl, postdata, "application/json", header);

            temporaryLogSentryService.WriteLogLaunchGame(nameof(FaChai), ApiUrl, header, postdata, result);

            ApiResult oApiResult = new ApiResult();

            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResp>(result);
                if (json.ErrorCode == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.Payload.ReturnUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.ErrorMessage;
                    oApiResult.RawErrorCode = json.ErrorCode.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public static string AesEncrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);
            System.Security.Cryptography.RijndaelManaged rm = new
            System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7,
            };
            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public enum FCErrorMessage
        {
            [Description("Success")]
            Success = 0,

            [Description("Withdraw points is 0")]
            Withdraw_points_is_0 = 200,

            [Description("Abnormal points")]
            Abnormal_points = 201,

            [Description("Wrong code for deposit and withdrawal")]
            Wrong_code_for_deposit_and_withdrawal = 202,

            [Description("Your points balance not enough to withdraw")]
            Your_points_balance_not_enough_to_withdraw = 203,

            [Description("Transaction ID number not exist")]
            Transaction_ID_number_not_exist = 221,

            [Description("Unable to obtain API customer (agent) key")]
            Unable_to_obtain_API_customer_agent_key = 301,

            [Description("Data decryption failed")]
            Data_decryption_failed = 303,

            [Description("API customer (agent) sign verification failed")]
            API_customer_agent_sign_verification_failed = 304,

            [Description("API customer (agent) sign is disabled")]
            API_customer_agent_sign_is_disabled = 401,

            [Description("This method is disabled")]
            This_method_is_disabled = 402,

            [Description("This method is under maintenance")]
            This_method_is_under_maintenance = 403,

            [Description("Game does not exist")]
            Game_does_not_exist = 405,

            [Description("Game is closed")]
            Game_is_closed = 406,

            [Description("Account locked")]
            Account_locked = 407,

            [Description("Game is under maintenance")]
            Game_is_under_maintenance = 408,

            [Description("No authorization for this account")]
            No_authorization_for_this_account = 409,

            [Description("IP not allowed")]
            IP_not_allowed = 410,

            [Description("Event not found")]
            Event_not_found = 416,

            [Description("Account does not exist")]
            Account_does_not_exist = 500,

            [Description("Account is too long")]
            Account_is_too_long = 501,

            [Description("Duplicate account")]
            Duplicate_account = 502,

            [Description("User is online")]
            User_is_online = 503,

            [Description("User is offline")]
            User_is_offline = 504,

            [Description("Account is too short")]
            Account_is_too_short = 505,

            [Description("URL is blank")]
            URL_is_blank = 601,

            [Description("API customer (agent) response is empty")]
            API_customer_agent_response_is_empty = 602,

            [Description("Return data is not in JSON format")]
            Return_data_is_not_in_JSON_format = 603,

            [Description("Verification failed")]
            Verification_failed = 604,

            [Description("No verification result is returned")]
            No_verification_result_is_returned = 605,

            [Description("Points obtain is not in numbers")]
            Points_obtain_is_not_in_numbers = 606,

            [Description("No points is returned")]
            No_points_is_returned = 607,

            [Description("Revert Cancel Bet")]
            Revert_Cancel_Bet = 799,
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return gameCode;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return (dic != null) ? dic.TryGetValue("en", out var result) ? result : gameCode : gameCode;
        }

        private string Encrypt(string req, FaChaiConfig config)
        {
            var s = req + config.OperatorKey;
            var sign = ToMD5(s);
            return sign;
        }

        private static string GetCurrentTimeFormat()
        {
            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            return currentAt.ToOffset(new TimeSpan(_utc, 0, 0)).ToString(_formatOfTime);
        }
    }
}