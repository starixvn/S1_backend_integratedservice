using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.AWC
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class SignupReq
        {
            public string cert { get; set; }
            public string agentId { get; set; }
            public string userId { get; set; }
            public string currency { get; set; }
            public string betLimit { get; set; }
            public string language { get; set; }
        }

        private class SignupResult
        {
            public string status { get; set; }
            public string desc { get; set; }
        }

        public class LoginReq
        {
            public string cert { get; set; }
            public string agentId { get; set; }
            public string userId { get; set; }
            public string isMobileLogin { get; set; }
            public string externalURL { get; set; }
            //public string gameForbidden { get; set; }
            public string gameType { get; set; }
            public string platform { get; set; }
            public string language { get; set; }
        }

        private class LoginResult
        {
            public string status { get; set; }
            public string url { get; set; }
        }

        class betLimit
        {
            public decimal maxbet { get; set; }
            public decimal minbet { get; set; }
            public decimal mindraw { get; set; }
            public decimal matchlimit { get; set; }
            public decimal maxdraw { get; set; }
        }

        Dictionary<string, betLimit> betLimitDev = new Dictionary<string, betLimit>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            { "MYR", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } },
            { "CNY", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } },
            { "IDR", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } },
            { "THB", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } },
            { "VND", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } },
            { "USD", new betLimit { minbet = 1, maxbet = 200, mindraw = 1, maxdraw = 100, matchlimit = 200 } }
        };

        Dictionary<string, betLimit> betLimitPrd = new Dictionary<string, betLimit>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            { "MYR", new betLimit { minbet = 5, maxbet = 1000, mindraw = 5, maxdraw = 125, matchlimit = 2000 } },
            { "CNY", new betLimit { minbet = 5, maxbet = 1500, mindraw = 5, maxdraw = 185, matchlimit = 3000 } },
            { "IDR", new betLimit { minbet = 15, maxbet = 3500, mindraw = 15, maxdraw = 435, matchlimit = 7000 } },
            { "THB", new betLimit { minbet = 35, maxbet = 7500, mindraw = 35, maxdraw = 935, matchlimit = 15000 } },
            { "VND", new betLimit { minbet = 25, maxbet = 5000, mindraw = 25, maxdraw = 625, matchlimit = 10000 } },
            { "USD", new betLimit { minbet = 1, maxbet = 250, mindraw = 1, maxdraw = 30, matchlimit = 500 } }
        };

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception("cert");
            string agentId = inputs.ContainsKey("agentId") && !string.IsNullOrEmpty(inputs["agentId"]) ? inputs["agentId"] : throw new Exception("agentId");
            ApiUrl = $"{ApiUrl}/wallet/createMember";

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            var betLimit = IsDevelopment ? betLimitDev[tpCustInfo.TpCurrencyId] : betLimitPrd[tpCustInfo.TpCurrencyId];

            SignupReq SignUp = new SignupReq
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                betLimit = JsonConvert.SerializeObject(new { SV388 = new { LIVE = betLimit } }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SignupResult>(result);
                if (json.status == "0000")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else if (json.status == "1001")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.desc;
                    oApiResult.RawErrorCode = json.status;
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception("cert");
            string agentId = inputs.ContainsKey("agentId") && !string.IsNullOrEmpty(inputs["agentId"]) ? inputs["agentId"] : throw new Exception("agentId");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "LobbyUrl";
            ApiUrl = $"{ApiUrl}/wallet/login";


            LoginReq Login = new LoginReq
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                isMobileLogin = (appType == AppType.Desktop) ? "false" : "true",
                externalURL = lobbyUrl,
                gameType = "LIVE",
                platform = "SV388",
                language = TpLangId,
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Login));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                if (json.status == "0000")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.url;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.status;
                    oApiResult.RawErrorCode = json.status;
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

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
