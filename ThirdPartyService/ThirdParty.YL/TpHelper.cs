using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.YL
{
    public class TpHelper : ThirdPartyHelperBase
    {
        readonly Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {   {0,"ail(etrmedbyGameProider)失败(由游戏提供商返回)"},
            {1,"Success成功"},
            {200,"Success成功"},
            {500,"Fa失败"},
            {1001,"NotAuthorized.未经授权。"},
            {1002,"Baanceisinvaid.无效余额。"},
            {1003,"NoData没有数据"},
            {1004,"Usenameisinvaid.无效的用户名。"},
            {1005,"Updatefaled.更新失败。"},
            {1006,"nsufcientparameters参数不足"},
            {1007,"NOtSupportedAPL.不支持APl"},
            {1008,"nvaidpaameters参数无效。"},
            {1009,"nsufcientbalance.余额不足"},
            {1010,"Actonaeadycompe行动已经完成。"},
            {1011,"Usermnbetormaxbetvolateagentbetseing.用户最小下注或最大下注违反代理下注设置。"},
            {1012,"Agentdoesnotexist.代理不存在。" },
            {1014,"QuerytoofastHitrateiritn查太快了命中率阳制。"},
            {1015,"nvalidURL无效的网"},
            {1016,"nvalidAgent无效的代"},
            {1017,"NoSettlment.没有解案"},
            {1018,"Accountloked.帐户被方琪"},
            {1019,"Noeventinthisdate.此明没王何活云"},
            {1020,"nvalidsinglewalletacti0n.单个包操作>改。"},
            {1023,"nvalidwebsite.网站无有钱登"},
            {1024,"nvalidcurrencysetting币设效。"},
            {1025,"Reachmaxcreditlmt到被重最用额度"},
            {1026,"Accounsuspended.帐暂停信录"},
            {1027,"AccOuntbere-login.帐新登"},
            {1028,"SystemMaintenance系主护"}
        };
        readonly Dictionary<int, string> extension1Dic = new Dictionary<int, string>() {
            {1,"yl00no01" },//MYR
            {9,"yl00no02" },//THB
            {6,"yl00no03" },//IDR
            {3,"yl00no04" },//CNY
            {11,"yl00no05" }//VND
        };
        public class UserReq
        {
            public string cert { get; set; }
            public string user { get; set; }
            public string userName { get; set; }
            public string extension1 { get; set; }
            public int currency { get; set; }
        }
        public class LoginReq
        {
            public string key { get; set; }
            public string language { get; set; }
            public int gameId { get; set; }
            public string user { get; set; }
            public string userName { get; set; }
            public string extension1 { get; set; }
            public string returnURL { get; set; }
        }

        public class UserResult
        {
            public int status { get; set; }
            public string key { get; set; }
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //throw new NotImplementedException();
            ApiResult oApiResult = new ApiResult();
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
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
            //throw new NotImplementedException();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception();
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "0";
            string getKeyUrl = $"{ApiUrl}getKey";

            UserReq userReq = new UserReq();
            userReq.user = tpCustInfo.TpUserName;
            userReq.userName = tpCustInfo.TpUserName;
            userReq.cert = cert;
            userReq.currency = Convert.ToInt32(tpCustInfo.TpCurrencyId);

            string curKey = $"CurId{tpCustInfo.TpCurrencyId}";
            if (inputs.ContainsKey(curKey))
            {
                userReq.extension1 = inputs[curKey];
            }

            var dicP = FromJson<Dictionary<string, string>>(ToJson(userReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(getKeyUrl, sParam, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<UserResult>(result);
                if (userResult.status == 1)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    LoginReq Login = new LoginReq();
                    Login.key = userResult.key;
                    Login.gameId = Convert.ToInt32(gameId);
                    Login.user = userReq.user;
                    Login.userName = userReq.userName;
                    Login.extension1 = userReq.extension1;
                    Login.language = TpLangId;
                    int.TryParse(gameId, out int iGameId);
                    Login.gameId = iGameId;
                    Login.returnURL = lobbyUrl;

                    dicP = FromJson<Dictionary<string, string>>(ToJson(Login));
                    sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={System.Web.HttpUtility.UrlEncode(kp.Value)}").ToArray());
                    if (appType != AppType.Desktop)
                    {
                        sParam = $"{sParam}&fullscreen=0";
                    }

                    oApiResult.Message = $"{ApiUrl}loginV2?{sParam}";
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = ErrorMassage[userResult.status];
                    oApiResult.RawErrorCode = userResult.status.ToString();
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.SystemError;
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
                var apiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception();
                var cert = inputs.GetValueOrDefault("cert") ?? throw new Exception();

                var url = $"{apiUrl}getGameReportByTxId";
                var sParam = $"cert={cert}&txId={getBetDetail.TransId}";

                var json = HttpPost(url, sParam, "application/x-www-form-urlencoded");
                var result = FromJson<DetailResult>(json);

                if (result.status != "1")
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status}");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = result.link,
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransFish_YL_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
            var gameCode = detail.GetStringValue("gameId");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetFishGameName(gameCode),
                RefNo = detail.GetStringValue("gameNumier"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("roomId"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        public class DetailResult
        {
            public string status { get; set; }
            public string link { get; set; }
        }

        private string GetFishGameName(string gameCode)
        {
            return gameCode switch
            {
                "10" => "Fishing 2",
                "11" => "Shark Hunter",
                _ => gameCode,
            };
        }
    }
}
