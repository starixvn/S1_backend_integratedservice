using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.MP
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        private readonly Dictionary<int, MPConfig> _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MPConfig>(TpEnum.MP.ToString());
        }

        #region ErrorCode
        readonly Dictionary<int, string> ErrorMassage = new()
        {
            { 1, "TOKEN 丢失" },
            { 2, "渠道不存在" },
            { 3, "验证时间超时" },
            { 4, "验证错误" },
            { 5, "渠道白名单错误" },
            { 6, "验证字段丢失" },
            { 8, "不存在的请求" },
            { 15, "渠道验证错误" },
            { 16, "数据不存在（当前没有注单）" },
            { 20, "账号禁用" },
            { 22, "AES 解密失败" },
            { 24, "渠道拉取数据超过时间范围" },
            { 26, "订单号不存在" },
            { 27, "数据库异常" },
            { 28, "ip 禁用" },
            { 29, "订单号与订单规则不符" },
            { 30, "获取玩家在线状态失败" },
            { 31, "更新的分数小于或者等于 0 " },
            { 32, "更新玩家信息失败" },
            { 33, "更新玩家金币失败" },
            { 34, "订单重复" },
            { 35, "获取玩家信息失败（请调用登录接口创建账号）" },
            { 36, "KindID 不存在" },
            { 37, "登录瞬间禁止下分，导致下分失败" },
            { 38, "余额不足导致下分失败" },
            { 39, "禁止同一账号登录带分、上分、下分并发请求，后一个请求被拒" },
            { 40, "单次上下分数量不能超过一百亿" },
            { 41, "拉取对局汇总统计时间范围有误" },
            { 42, "代理被禁用" },
            { 43, "拉单过于频繁(两次拉单时间间隔必须大于 1 秒)" },
            { 44, "订单正在处理中" },
            { 999, "请求失败" },
            { 1001, "注册会员账号系统异常" },
            { 1002, "代理商金额不足" },
            { 1003, "玩家大厅上分/下分异常" },
            { 1004, "代理商渠道不存在" },
            { 1005, "会员金额不足" },
            { 1006, "会员游戏上分/下分异常" },
            { 1007, "玩家登录游戏上分异常" },
            { 1008, "玩家注销游戏下分异常" },
            { 1009, "上下分出现负数(非法值)" },
            { 1010, "会员退出大厅异常" },
            { 1011, "订单已存在" },
            { 1012, "订单号不符合规则" },
            { 1013, "后台上分/下分异常" },
            { 1014, "上级代理金额不足" },
            { 1015, "会员代理充值异常(不能跨代理给会员充值) " },
            { 1016, "玩家账户被锁" },
            { 1017, "更新会员、浏览器信息异常" },
            { 1018, "存储过程参数格式不正确或参数为空" },
            { 1019, "更新会员玩过的游戏信息异常" },
            { 1020, "玩家大厅进(出)游戏上分(下分)生成订单异常" },
            { 1021, "会员订单失效或不存在" },
            { 1022, "会员订单已经处理过，不能重复处理" },
            { 1023, "玩家正在游戏中，不能上下分" },
            { 1024, "大厅会员账号不存在" },
            { 1025, "参数错误，子渠道标识不能为空" },
        };
        #endregion

        #region Response
        private class ApiResponse
        {
            public int s { get; set; }
            public string m { get; set; }
            public Info d { get; set; }
        }

        public class Info
        {
            public int code { get; set; }
            public string url { get; set; }
            public decimal totalMoney { get; set; }
            public decimal freeMoney { get; set; }
            public int status { get; set; }
            public string data { get; set; }
        }
        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            var config = _siteConfig[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string Agent = config.Agent;
            string Deskey = config.Deskey;
            string Md5key = config.Md5key;

            var current = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = current.ToUnixTimeMilliseconds();

            var Param = new
            {
                s = 4,
                orderid = ThirdPartyTransID
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var CheckTransfer = new
            {
                agent = Agent,
                timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(CheckTransfer));
            var sCheckTransfer = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(
                ApiUrl,
                sCheckTransfer,
                "application/x-www-form-urlencoded"
            );

            ApiResult<TransferResult> apiResult = new();
            if (!IsJSON(result))
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
                return apiResult;
            }

            var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            if (json.d.code != 0)
            {
                apiResult.RawMessage = ErrorMassage[json.d.code];
                apiResult.RawErrorCode = json.d.code.ToString();
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }
            else
            {
                if (json.d.status == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = TransID;
                }
                else if (json.d.status == 3)
                {
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                    apiResult.Data.ThirdId = TransID;
                }
                else
                {
                    apiResult.RawMessage = ErrorMassage[json.d.code];
                    apiResult.RawErrorCode = json.d.code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
            }
            return apiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            ApiResult oApiResult = new()
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new()
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new()
            {
                Implemented = true
            };
            return transIdModel;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfig[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string Agent = config.Agent;
            string Deskey = config.Deskey;
            string Md5key = config.Md5key;
            string gameId = inputs.GetValueOrDefault("gameId", "");

            var current = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = current.ToUnixTimeMilliseconds();
            var timecnv = current.ToString("yyyyMMddHHmmssfff");
            var Param = new
            {
                s = 0,
                account = tpCustInfo.TpUserName,
                money = 0,
                orderid = $"{Agent}{timecnv}{tpCustInfo.TpUserName}",
                ip = "",
                lineCode = tpCustInfo.SiteId,
                KindID = gameId
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));

            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var Login = new
            {
                agent = Agent,
                timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(Login));

            var sLogin = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(
                ApiUrl,
                sLogin,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new();
            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            if (json.d.code == 0)
            {
                //var returnType = (string.IsNullOrEmpty(HomeUrl)) ? 0 : 1;
                oApiResult.ErrorCode = ErrorCode.Success;
                var forward = json.d.url;
                var uri = new Uri(forward);
                string pLang = HttpUtility.ParseQueryString(uri.Query).Get("lang");
                if (pLang != TpLangId)
                {
                    forward = forward.Replace($"lang={pLang}", $"lang={TpLangId}");
                }
                //oApiResult.Message = $"{forward}&returnUrl={HomeUrl}&returnType={returnType}";
                oApiResult.Message = forward;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = ErrorMassage[json.d.code];
                oApiResult.RawErrorCode = json.d.code.ToString();
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            var config = _siteConfig[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string Agent = config.Agent;
            string Deskey = config.Deskey;
            string Md5key = config.Md5key;

            var current = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = current.ToUnixTimeMilliseconds();
            var timecnv = current.ToString("yyyyMMddHHmmssfff");
            int transType = (direction == Direction.ToMain) ? 3 : 2;

            var Param = new
            {
                s = transType,
                account = tpCustInfo.TpUserName,
                money = Amount,
                orderid = $"{Agent}{timecnv}{tpCustInfo.TpUserName}"//$"{Agent}{TransID}"
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var Transfer = new
            {
                agent = Agent,
                timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(Transfer));
            var sTransfer = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(
                ApiUrl,
                sTransfer,
                "application/x-www-form-urlencoded"
            );

            ApiResult<TransferResult> apiResult = new();
            apiResult.RawMessage = "No Result";
            apiResult.RawErrorCode = "1";
            apiResult.ErrorCode = ErrorCode.TransferPending;
            apiResult.Data.Status = TransferStatus.Pending;
            apiResult.Data.ThirdId = Param.orderid;

            if (!IsJSON(result))
            {
                return apiResult;
            }

            var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            if (json.d.code == 0)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Data.Status = TransferStatus.Success;
                apiResult.RawMessage = "";
                apiResult.RawErrorCode = "";
            }
            else
            {
                if (json.d.code == 27)
                {
                    // 数据库异常
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                apiResult.RawMessage = ErrorMassage[json.d.code] ?? "";
                apiResult.RawErrorCode = json.d.code.ToString() ?? "";
            }
            return apiResult;
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfig[tpCustInfo.SiteId];
            string ApiUrl = config.ApiUrl;
            string Agent = config.Agent;
            string Deskey = config.Deskey;
            string Md5key = config.Md5key;

            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var Param = new
            {
                s = 8,
                account = tpCustInfo.TpUserName
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var Logout = new
            {
                agent = Agent,
                timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(Logout));
            var sLogout = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(
                ApiUrl,
                sLogout,
                "application/x-www-form-urlencoded"
            );

            ApiResult oApiResult = new();
            if (!IsJSON(result))
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
                return oApiResult;
            }

            var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            if (json.d.code == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = _siteConfig[tpCustInfo.SiteId];
            string WagerUrl = config.WagerUrl;
            string Agent = config.Agent;
            string Deskey = config.Deskey;
            string Md5key = config.Md5key;

            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerPK_MP_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            Dictionary<string, string> keyValuePairs = new();
            foreach (var item in wagerDetail.Select((val, i) => new { i, val }))
            {
                keyValuePairs.Add(item.val.Key, item.val.Value.ToString());
            }

            var Param = new
            {
                s = 10,
                account = wagerDetail.GetValueOrDefault("Accounts")?.ToString(),
                gameUserNO = wagerDetail.GetValueOrDefault("GameID")?.ToString(),
                kindID = wagerDetail.GetValueOrDefault("KindID")?.ToString(),
            };
            var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));

            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var GetDetail = new
            {
                agent = Agent,
                timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower(),
                lang = "en"
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(GetDetail));

            var sDetail = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(
                WagerUrl,
                sDetail,
                "application/x-www-form-urlencoded"
            );

            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (json.d.code == 0)
                {
                    var data = json.d.data.Split("\r\n\r\n");
                    var element = (string.IsNullOrEmpty(data.ElementAtOrDefault(1))) ? data.ElementAtOrDefault(0) : data.ElementAtOrDefault(1);
                    var info = (!string.IsNullOrEmpty(element)) ? element : json.d.data;
                    keyValuePairs.Add("Info", info);
                }
            }

            var detail = ToJson(keyValuePairs);

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.P2P => new PokerDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("TableID")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("Accounts")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("GameStartTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("GameEndTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("AllBet") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("Profit") * tpCurrency.ExchangeRate,
                    GameCode = wagerDetail.GetValueOrDefault("KindID")?.ToString(),
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerPK_MP_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
            var gameCode = detail.GetStringValue("KindID");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detail.GetStringValue("GameID"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("TableID"),
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }

        private static string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "900" => "Dragon Tiger",
                "910" => "Baccarat",
                "960" => "Hoo Hey How",
                "502" => "Hilo",
                "180" => "Pok Deng",
                "600" => "Blackjack",
                "220" => "Golden Flower",
                "620" => "Texas Hold'em",
                "830" => "Banker Niu-Niu",
                "501" => "Gao Gae",
                "504" => "Paikang",
                "503" => "Dummy",
                "505" => "Domino",
                "506" => "Domino Qiu Qiu",
                "509" => "Thai Hold'em",
                "508" => "Crazy Paikang",
                "510" => "Cockfighting",
                "512" => "Teen Patti",
                "1000" => "Lucky Wheel",
                "514" => "Xoc Dia",
                "516" => "Killer13",
                "517" => "Catte",
                "519" => "Rummy",
                "515" => "Fan Tan",
                "181" => "MP Bo Deng",
                "182" => "MP Pokdeng Card",
                _ => string.Empty,
            };
        }
    }
}
