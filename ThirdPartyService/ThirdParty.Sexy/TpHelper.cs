using System;
using System.Collections.Generic;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using System.Linq;
using System.Web;

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
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string cert = inputs.ContainsKey("cert") && !string.IsNullOrEmpty(inputs["cert"]) ? inputs["cert"] : throw new Exception("cert");
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "0";
            string WEBSITE = inputs.ContainsKey("WEBSITE") && !string.IsNullOrEmpty(inputs["WEBSITE"]) ? inputs["WEBSITE"] : throw new Exception("WEBSITE");
            string getKeyUrl = $"{ApiUrl}api/{WEBSITE}/getKey";

            GetKeyReq getKeyReq = new GetKeyReq();

            string betLimitIdKey = $"BetLimitId{tpCustInfo.TpCurrencyId}";
            string agentIdKey = $"AgentId{tpCustInfo.TpCurrencyId}";
            getKeyReq.cert = cert;
            getKeyReq.currency = currencyIdMapping[tpCustInfo.TpCurrencyId];
            getKeyReq.user = tpCustInfo.TpUserName;
            getKeyReq.betLimitIds = inputs[betLimitIdKey];

            var dicP = FromJson<Dictionary<string, string>>(ToJson(getKeyReq));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPost(getKeyUrl, sParam, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var getKeyResp = FromJson<GetKeyResp>(result);
                if (getKeyResp.Status == 1)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    LoginReq loginReq = new LoginReq();
                    loginReq.key = getKeyResp.Key;
                    loginReq.user = getKeyReq.user;
                    loginReq.userName = getKeyReq.user;
                    loginReq.extension1 = inputs[agentIdKey];
                    loginReq.extension2 = loginReq.extension1;
                    loginReq.language = TpLangId;
                    loginReq.lobbyURL = lobbyUrl;
                    loginReq.mb = appType == AppType.Desktop ? null : "1";

                    dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
                    sParam = string.Join("&", dicP.Where(kp => !string.IsNullOrEmpty(kp.Value)).Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());
                    oApiResult.Message = $"{getKeyResp.Url}{sParam}";
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = ErrorMassage[getKeyResp.Status];
                    oApiResult.RawErrorCode = getKeyResp.Status.ToString();
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
    }
}
