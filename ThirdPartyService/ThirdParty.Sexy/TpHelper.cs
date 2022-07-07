using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Extensions;

namespace ThirdParty.Sexy
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<string, int> currencyIdMapping = new Dictionary<string, int>() {
            {"MYR",1 },
            {"CNY",3 },
            {"IDR",6 },
            {"THB",9 },
            {"VND",11 },
            {"INR",15 },
        };

        private readonly Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {
            {1,"Success成功"},
            {200,"Success成功"},
            {1001,"Not Authorized.未经授权。"},
            {1002,"Balance is invalid. 余额无效。"},
            {1003,"NoData没有数据"},
            {1004,"Usename is invaid.无效的用户名。"},
            {1005,"Update faled.更新失败。"},
            {1006,"insufcient parameters.参数不足"},
            {1007,"Not supported API.不支持APl"},
            {1008,"invaid paameters.参数无效。"},
            {1009,"Insufficient balance. 余额不足。"},
            {1010,"Action already completed. 行动已经完成。"},
            {1011,"User min bet or max bet violate agent bet setting. 用户最小下注或最大下注违反代理下注设置。"},
            {1012,"Agent does not exist. 代理不存在。" },
            {1013,"User currency type not match agent setting. 用户货币类型与代理商设置不匹配。" },
            {1014,"Query too fast. Hit rate limit. 查询太快了。 命中率限制。"},
            {1015,"Invalid URL. 无效的网址。"},
            {1016,"Invalid Agent. 无效的代理。"},
            {1017,"No Settlement. 没有解决方案。"},
            {1018,"Account locked. 帐户被锁定。"},
            {1019,"No event in this date. 此日期没有任何活动。"},
            {1020,"Invalid single wallet action. 单个钱包操作无效。"},
            {1021,"Overdue match status. 逾期匹配状态。"},
            {1022,"Reach max win limit. 达到最大赢额限制。"},
            {1023,"Invalid website. 网站无效。"},
            {1024,"Invalid currency setting. 货币设置无效。"},
            {1025,"Reach max credit limit. 达到最高信用额度。"},
            {1026,"Account suspended. 帐户被暂停"},
            {1027,"Account be re-login. 帐户重新登录。"},
            {1028,"Agent incorrect. 代理不正确。"},
            {1029,"Invalid parameters. Please check currency & betlimitid\" 参数无效。 请检查货币和限红\""},
            {1030,"Invalid betlimitId setting. 限红设置无效。"},
        };

        private class SignupResponse
        {
            public string status { get; set; }
            public string desc { get; set; }
        }

        private class LoginResponse
        {
            public string status { get; set; }
            public string desc { get; set; }
            public string url { get; set; }
        }

        public class GetKeyReq
        {
            public string cert { get; set; }
            public string user { get; set; }
            public string betLimitIds { get; set; }
            public int currency { get; set; }
        }

        public class GetKeyResp
        {
            public int Status { get; set; }
            public string Key { get; set; }
            public string Url { get; set; }
        }

        public class LoginReq
        {
            public string user { get; set; }
            public string userName { get; set; }
            public string key { get; set; }
            public string language { get; set; }
            public string mb { get; set; }
            public string extension1 { get; set; }
            public string extension2 { get; set; }
            public string lobbyURL { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl2") && !string.IsNullOrEmpty(inputs["ApiUrl2"]) ? inputs["ApiUrl2"] : throw new Exception("ApiUrl2");
            string cert = inputs.ContainsKey("cert2") && !string.IsNullOrEmpty(inputs["cert2"]) ? inputs["cert2"] : throw new Exception("cert2");
            string agentId = inputs.ContainsKey("AgentId") && !string.IsNullOrEmpty(inputs["AgentId"]) ? inputs["AgentId"] : throw new Exception("AgentId");
            string betLimitId = inputs.ContainsKey($"BetLimitId2{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"BetLimitId2{tpCustInfo.TpCurrencyId}"]) ? inputs[$"BetLimitId2{tpCustInfo.TpCurrencyId}"] : throw new Exception($"BetLimitId2{tpCustInfo.TpCurrencyId}");
            ApiUrl = $"{ApiUrl}/wallet/createMember";

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            var SignUp = new
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                betLimit = JsonConvert.SerializeObject(new
                {
                    SEXYBCRT = new
                    {
                        LIVE = new
                        {
                            limitId = new List<string>()
                                    {
                                       betLimitId
                                    }
                        }
                    }
                }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<SignupResponse>(result);
            if (json.status == "0000")
            {
                oApiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.desc;
                oApiResult.RawErrorCode = json.status;
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
            string ApiUrl = inputs.ContainsKey("ApiUrl2") && !string.IsNullOrEmpty(inputs["ApiUrl2"]) ? inputs["ApiUrl2"] : throw new Exception("ApiUrl2");
            string cert = inputs.ContainsKey("cert2") && !string.IsNullOrEmpty(inputs["cert2"]) ? inputs["cert2"] : throw new Exception("cert2");
            string agentId = inputs.ContainsKey("AgentId") && !string.IsNullOrEmpty(inputs["AgentId"]) ? inputs["AgentId"] : throw new Exception("AgentId");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "LobbyUrl";
            string betLimitId = inputs.ContainsKey($"BetLimitId2{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"BetLimitId2{tpCustInfo.TpCurrencyId}"]) ? inputs[$"BetLimitId2{tpCustInfo.TpCurrencyId}"] : throw new Exception($"BetLimitId2{tpCustInfo.TpCurrencyId}");
            ApiUrl = $"{ApiUrl}/wallet/doLoginAndLaunchGame";

            if (!IsDevelopment && tpCustInfo.CustId == 61920)
            {
                /* 
                 Nova88_61920  IDR member can change his bet limit to 5-500
                */
                betLimitId = "580601";
            }

            var Login = new
            {
                cert = cert,
                agentId = agentId,
                userId = tpCustInfo.TpUserName,
                isMobileLogin = (appType == AppType.Desktop) ? "false" : "true",
                externalURL = lobbyUrl,
                gameType = "LIVE",
                gameCode = "MX-LIVE-001",
                platform = "SEXYBCRT",
                language = TpLangId,
                hall = "SEXY",
                betLimit = JsonConvert.SerializeObject(new
                {
                    SEXYBCRT = new
                    {
                        LIVE = new
                        {
                            limitId = new List<string>()
                        {
                            betLimitId
                        }
                        }
                    }
                }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Login));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new ApiResult();

            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);
            if (json.status == "0000")
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = json.url;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = json.desc;
                oApiResult.RawErrorCode = json.status;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                var apiUrl = inputs.GetValueOrDefault("ApiUrl2") ?? throw new Exception();
                var cert = inputs.GetValueOrDefault("cert2") ?? throw new Exception();
                var agentId = inputs.GetValueOrDefault("AgentId") ?? throw new Exception();

                var url = $"{apiUrl}/wallet/getTransactionHistoryResult";
                var sParam = $"cert={cert}&agentId={agentId}&userId={tpCustInfo.TpUserName}&platformTxId={getBetDetail.TransId}&platform=SEXYBCRT";

                var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
                var result = FromJson<DetailResult>(json);

                if (result.status != "0000")
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status}");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.txnUrl,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            catch (Exception e)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {e.Message}");
            }
        }
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransLC_AWC_Sexy_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
            var bet = JsonConvert.DeserializeObject<DetailSexy>(detail.GetStringValue("gameInfo"));
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("gameCode"),
                    Game = detail.GetStringValue("gameName"),
                    RefNo = detail.GetStringValue("refPlatformTxId"),
                    Title = detail.GetStringValue("gameType"),
                    MatchID = detail.GetStringValue("roundId"),
                    BetType = detail.GetStringValue("betType"),
                    BetChoice = string.Empty,
                    Result = JsonConvert.SerializeObject(bet.result),
                    Odds = decimal.TryParse(bet.odds, out var odds) ? odds : 0,
                    DeviceType = DeviceType.None,
                    IP = bet.ip ?? string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }
        public class DetailResult
        {
            public string url { get; set; }
            public string txnUrl { get; set; }
            public string roundUrl { get; set; }
            public string status { get; set; }
        }

        public class DetailSexy
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public dynamic? result { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? ip { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? odds { get; set; }
        }

    }
}
