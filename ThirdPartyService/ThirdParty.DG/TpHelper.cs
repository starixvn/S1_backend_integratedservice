using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.DG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly DGConfig _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<DGConfig>(TpEnum.DG.ToString());
        }

        Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {   {1,"Parameter Error"},
            {2,"Token Verification Failed"},
            {3,"Command Not Find"},
            {4,"Illegal Operation"},
            {10,"Date format error"},
            {11,"Data format error"},
            {97,"Permission denied"},
            {98,"Operation failed"},
            {99,"Unknown Error"},
            {100,"Account is locked"},
            {101,"Account format error"},
            {102,"Account does not exist"},
            {103,"This account is taken"},
            {104,"Password format error"},
            {105,"Password wrong"},
            {106,"New & Old Password is the same"},
            {107,"Member account unavailable"},
            {108,"Login Error"},
            {109,"Signup Error"},
            {110,"This account has been signed in"},
            {111,"This account has been signed out"},
            {112,"This account is not signed in"},
            {113,"The Agent account inputted is not an Agent account"},
            {114,"Member not found"},
            {116,"Account occupied"},
            {117,"Can not find branch of member"},
            {118,"Can not find the specified Agent"},
            {119,"Insufficent funds during Agent withdrawal"},
            {120,"Insufficient balance"},
            {121,"Profit limit must be greater than or equal to 0"},
            {150,"Ran out of free demo accounts"},
            {300,"system maintenance"},
            {320,"Wrong API key"},
            {321,"Limit Group Not Found"},
            {322,"Currency Name Not Found"},
            {323,"Use serial numbers for Transfer"},
            {324,"Transfer failed"},
            {325,"Agent Status Unavailable"},
            {326,"Members Agent No video group"},
            {400,"Client IP Restricted"},
            {401,"Network latency"},
            {402,"The connection is closed"},
            {403,"Clients limited sources"},
            {404,"Resource requested does not exist"},
            {405,"Too frequent requests"},
            {406,"Can't get balance by wallet API"},
            {407,"Can not find game address"},
            {500,"Null pointer exception"},
            {501,"System Error"},
            {502,"The system is busy"},
            {503,"Data operation error"}
        };

        Dictionary<string, string> limitGroupNoMapping = new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            {"MYR","G"},
            {"THB","N"},
            {"IDR2","G"},
            {"VND2","G"},
            {"CNY","G"},
            {"USD","F"},
            {"INR","G"},
            {"BDT","A"},
            {"JPY","G"},
            {"KRW2","F"},
            {"BRL","B"}
        };

        private decimal dWinLimit = 0m;
        private const string DefaultLimitGroup = "A";

        #region Request
        public class Member
        {
            public string username { get; set; }
            public string password { get; set; }
        }
        public class CommReq
        {
            public string token { get; set; }
            public string random { get; set; }
        }
        public class SignupReq : CommReq
        {
            public string data { get; set; }
            public SignupMember member { get; set; }
        }
        public class LoginReq : CommReq
        {
            public string lang { get; set; }
            public string domains { get; set; }
            public Member member { get; set; }
            public string data { get; set; }
        }
        public class SignupMember : Member
        {
            public string currencyName { get; set; }
            public decimal winLimit { get; set; }
        }
        #endregion

        #region Response
        private class SignupResult : CommReq
        {
            public int codeId { get; set; }
            public string data { get; set; }
        }

        private class LoginResult : CommReq
        {
            public int codeId { get; set; }
            public string domains { get; set; }
            public string[] list { get; set; }
        }
        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfig;

            string ApiUrl = config.ApiUrl;
            string AgentName = config.AgentAccount;
            string APIkey = config.APIkey;
            ApiUrl = $"{ApiUrl}/user/signup/{AgentName}";

            SignupReq SignUp = new SignupReq();
            SignUp.member = new SignupMember();

            string Random = GetRandomString(8);
            string MD5Key = ToMD5($"{AgentName}{APIkey}{Random}").ToLower();
            SignUp.token = MD5Key;
            SignUp.random = Random;
            SignUp.data = limitGroupNoMapping.ContainsKey(tpCustInfo.TpCurrencyId)
                ? limitGroupNoMapping[tpCustInfo.TpCurrencyId]
                : DefaultLimitGroup;
            SignUp.member.username = tpCustInfo.TpUserName;
            SignUp.member.password = tpCustInfo.TpPwd;
            SignUp.member.currencyName = tpCustInfo.TpCurrencyId;
            SignUp.member.winLimit = dWinLimit;

            string InputJson = JsonConvert.SerializeObject(SignUp);

            var result = HttpPost(ApiUrl, InputJson, "application/json");
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SignupResult>(result);
                if (json.codeId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = ErrorMassage[json.codeId];
                    oApiResult.RawErrorCode = json.codeId.ToString();
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
            var config = _siteConfig;

            string ApiUrl = config.ApiUrl;
            string AgentName = config.AgentAccount;
            string APIkey = config.APIkey;
            ApiUrl = $"{ApiUrl}/user/login/{AgentName}";

            LoginReq Login = new LoginReq();
            Login.member = new Member();

            string Random = GetRandomString(8);
            string MD5Key = ToMD5($"{AgentName}{APIkey}{Random}").ToLower();
            Login.token = MD5Key;
            Login.random = Random;
            Login.lang = TpLangId;
            Login.domains = "";

            Login.member.username = tpCustInfo.TpUserName.ToUpper();
            Login.member.password = tpCustInfo.TpPwd;
            Login.data = GetLimitGroup(tpCustInfo.TpCurrencyId, tpCustInfo.CustId);

            string InputJson = JsonConvert.SerializeObject(Login);

            var result = HttpPost(ApiUrl, InputJson, "application/json");
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                if (json.codeId == 0)
                {
                    if (appType == AppType.Desktop)
                    {
                        oApiResult.Message = $"{json.list[0]}{json.token}&language={TpLangId}";
                    }
                    else
                    {
                        oApiResult.Message = $"{json.list[1]}{json.token}&language={TpLangId}";
                    }
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = ErrorMassage[json.codeId];
                    oApiResult.RawErrorCode = json.codeId.ToString();
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_DG_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("tableId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("userName")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("betTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("calTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("betPoints") * tpCurrency.ExchangeRate,
                    WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("winOrLoss") - (decimal)wagerDetail.GetValueOrDefault("betPoints")) * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("gameId")?.ToString(),
                    GameName = string.Empty,
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_DG_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("gameId"),
                    Game = tpGameType.TypeName,
                    RefNo = detail.GetStringValue("id"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("tableId"),
                    BetType = string.Empty,
                    BetChoice = detail.GetStringValue("betDetail"),
                    Result = detail.GetStringValue("result"),
                    DeviceType = GetDeviceType(detail.GetStringValue("deviceType")),
                    IP = detail.GetStringValue("ip"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private static DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "1" => DeviceType.Desktop,
                "2" => DeviceType.iOS,
                "3" => DeviceType.Android,
                "5" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }

        private string GetLimitGroup(string tpCurrencyId, int custId)
        {
            var custBetLimit = BetLimitProvider.GetCustBetLimit(custId);
            if (custBetLimit != null)
            {
                var betSetting = JsonConvert.DeserializeObject<List<BetSetting>>(custBetLimit.BetSetting);
                var dg = betSetting?.SingleOrDefault(a => a.GameTypeId == (int)TpGameType.DG_LiveCasino);
                if (dg != null)
                    return dg.LimitGroup;
            }

            if (limitGroupNoMapping.ContainsKey(tpCurrencyId))
            {
                return limitGroupNoMapping[tpCurrencyId];
            }
            else
            {
                return DefaultLimitGroup;
            }
        }

        private class BetSetting
        {
            public int GameTypeId { get; set; }
            public string LimitGroup { get; set; }
        }
    }
}
