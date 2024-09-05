using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Sexy
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, Dictionary<string, string>> _siteIdConfigDic;

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

        public TpHelper(IServiceProvider provider)
        {            
            _siteIdConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.Sexy.ToString());
        }

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

        private class SexyConfig
        {
            public string ApiUrl { get; set; }

            public string Cert { get; set; }

            public string AgentId { get; set; }

            public List<string> BetLimitIds
            {
                get
                {
                    return new List<string>()
                  {
                      BetLimitId,
                      BetLimitId2
                  }.Where(r => !string.IsNullOrEmpty(r)).ToList();
                }
                set { BetLimitIds = value; }
            }

            private string BetLimitId { get; }

            private string BetLimitId2 { get; }

            public SexyConfig(Dictionary<string, string> inputs, Dictionary<string, string> siteConfig, int siteId, string tpCurrencyId = "")
            {
                ApiUrl = siteConfig.ContainsKey("ApiUrl2") && !string.IsNullOrEmpty(siteConfig["ApiUrl2"]) ? siteConfig["ApiUrl2"] : throw new Exception("ApiUrl2");
                Cert = siteConfig.ContainsKey("cert2") && !string.IsNullOrEmpty(siteConfig["cert2"]) ? siteConfig["cert2"] : throw new Exception("cert2");
                AgentId = siteConfig.ContainsKey("AgentId") && !string.IsNullOrEmpty(siteConfig["AgentId"]) ? siteConfig["AgentId"] : throw new Exception("AgentId");

                if (string.IsNullOrEmpty(tpCurrencyId)) return;
                BetLimitId = siteConfig.ContainsKey($"BetLimitId{tpCurrencyId}") ? siteConfig[$"BetLimitId{tpCurrencyId}"] : string.Empty;
                BetLimitId2 = siteConfig.ContainsKey($"BetLimitId2{tpCurrencyId}") ? siteConfig[$"BetLimitId2{tpCurrencyId}"] : string.Empty;
            }
        }

        private class SpecialBetLimitData
        {
            public int CustId { get; set; }
            public string BetLimitId { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteIdConfigDic[tpCustInfo.SiteId];
            var config = new SexyConfig(inputs, siteConfig, tpCustInfo.SiteId, tpCustInfo.TpCurrencyId);
            var apiUrl = $"{config.ApiUrl}/wallet/createMember";
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            var signUp = new
            {
                cert = config.Cert,
                agentId = config.AgentId,
                userId = tpCustInfo.TpUserName,
                currency = tpCustInfo.TpCurrencyId,
                betLimit = JsonConvert.SerializeObject(new
                {
                    SEXYBCRT = new
                    {
                        LIVE = new
                        {
                            limitId = config.BetLimitIds
                        }
                    }
                }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(signUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                apiUrl,
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
            var siteConfig = _siteIdConfigDic[tpCustInfo.SiteId];
            var lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "LobbyUrl";
            var platform = siteConfig.GetValueOrDefault("Platform") ?? throw new Exception("Platform");
            var gameType = siteConfig.GetValueOrDefault("GameType") ?? throw new Exception("GameType");
            var specialBetLimit = siteConfig.GetValueOrDefault("SpecialBetLimit") ?? null;

            var config = new SexyConfig(inputs, siteConfig, tpCustInfo.SiteId, tpCustInfo.TpCurrencyId);
            var apiUrl = $"{config.ApiUrl}/wallet/doLoginAndLaunchGame";

            if (specialBetLimit != null)
            {
                var specialBetLimitData = JsonConvert.DeserializeObject<IEnumerable<SpecialBetLimitData>>(specialBetLimit);
                if (specialBetLimitData.Any(x => x.CustId == tpCustInfo.CustId))
                {
                    var specialBetLimitCustId = specialBetLimitData.First(x => x.CustId == tpCustInfo.CustId);
                    siteConfig[$"BetLimitId{tpCustInfo.TpCurrencyId}"] = specialBetLimitCustId.BetLimitId;
                    siteConfig[$"BetLimitId2{tpCustInfo.TpCurrencyId}"] = "";
                    config = new SexyConfig(inputs, siteConfig, tpCustInfo.SiteId, tpCustInfo.TpCurrencyId);
                }
            }

            var login = new
            {
                cert = config.Cert,
                agentId = config.AgentId,
                userId = tpCustInfo.TpUserName,
                isMobileLogin = (appType == AppType.Desktop) ? "false" : "true",
                externalURL = lobbyUrl,
                gameType = gameType,
                gameCode = "MX-LIVE-001",
                platform = platform,
                language = TpLangId,
                hall = "SEXY",
                betLimit = JsonConvert.SerializeObject(new
                {
                    SEXYBCRT = new
                    {
                        LIVE = new
                        {
                            limitId = config.BetLimitIds
                        }
                    }
                }),
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(login));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                apiUrl,
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
                var siteConfig = _siteIdConfigDic[tpCustInfo.SiteId];
                var config = new SexyConfig(inputs, siteConfig, tpCustInfo.SiteId);
                var apiUrl = config.ApiUrl;
                var cert = config.Cert;
                var agentId = config.AgentId;
                //var platform = inputs.GetValueOrDefault("Platform") ?? throw new Exception();
                var platform = siteConfig.GetValueOrDefault("Platform") ?? throw new Exception("Platform");

                var url = $"{apiUrl}/wallet/getTransactionHistoryResult";
                var sParam = $"cert={cert}&agentId={agentId}&userId={tpCustInfo.TpUserName}&platformTxId={getBetDetail.TransId}&platform={platform}";

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
                    BetType = string.Empty,
                    BetChoice = detail.GetStringValue("betType"),
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
