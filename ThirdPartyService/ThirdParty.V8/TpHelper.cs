using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.V8
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, V8Config> _siteConfig;
        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<V8Config>(TpEnum.V8.ToString());
        }

        Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {   {1,"TOKEN 丢失"},
            {2,"渠道不存在"},
            {3,"验证时间超时"},
            {4,"验证错误"},
            {5,"渠道白名单错误"},
            {6,"验证字段丢失"},
            {8,"不存在的请求"},
            {15,"渠道验证错误"},
            {16,"数据不存在（当前没有注单）"},
            {20,"账号禁用"},
            {22,"AES 解密失败"},
            {24,"渠道拉取数据超过时间范围"},
            {26,"订单号不存在"},
            {27,"数据库异常"},
            {28,"ip 禁用"},
            {29,"订单号与订单规则不符"},
            {30,"获取玩家在线状态失败"},
            {31,"更新的分数小于或者等于 0 "},
            {32,"更新玩家信息失败"},
            {33,"更新玩家金币失败"},
            {34,"订单重复"},
            {35,"获取玩家信息失败（请调用登录接口创建账号）"},
            {36,"KindID 不存在"},
            {37,"登录瞬间禁止下分，导致下分失败"},
            {38,"余额不足导致下分失败"},
            {39,"禁止同一账号登录带分、上分、下分并发请求，后一个请求被拒"},
            {40,"单次上下分数量不能超过一百亿"},
            {41,"拉取对局汇总统计时间范围有误"},
            {42,"代理被禁用"},
            {43,"拉单过于频繁(两次拉单时间间隔必须大于 1 秒)"},
            {999,"请求失败"},
            {1001,"注册会员账号系统异常"},
            {1002,"代理商金额不足"},
            {1003,"玩家大厅上分/下分异常"},
            {1004,"代理商渠道不存在"},
            {1005,"会员金额不足"},
            {1006,"会员游戏上分/下分异常"},
            {1007,"玩家登录游戏上分异常"},
            {1008,"玩家注销游戏下分异常"},
            {1009,"上下分出现负数(非法值)"},
            {1010,"会员退出大厅异常"},
            {1011,"订单已存在"},
            {1012,"订单号不符合规则"},
            {1013,"后台上分/下分异常"},
            {1014,"上级代理金额不足"},
            {1015,"会员代理充值异常(不能跨代理给会员充值) "},
            {1016,"玩家账户被锁"},
            {1017,"更新会员、浏览器信息异常"},
            {1018,"存储过程参数格式不正确或参数为空"},
            {1019,"更新会员玩过的游戏信息异常"},
            {1020,"玩家大厅进(出)游戏上分(下分)生成订单异常"},
            {1021,"会员订单失效或不存在"},
            {1022,"会员订单已经处理过，不能重复处理"},
            {1023,"玩家正在游戏中，不能上下分"},
            {1024,"大厅会员账号不存在"},
            {1025,"参数错误，子渠道标识不能为空"},
        };

        private class ApiResponse
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
            public string url { get; set; }
            public decimal totalMoney { get; set; }
            public decimal freeMoney { get; set; }
            public int status { get; set; }
            public string extraParam { get; set; }
            public string lang { get; set; }
        }
        public class CurrencyData
        {
            public string Deskey { get; set; }
            public string Agent { get; set; }
            public string Md5key { get; set; }
        }
        public class V8Config
        {
            public string WagerUrl { get; set; }
            public string ApiUrl { get; set; }
            public Dictionary<string, CurrencyData> CurrencyData { get; set; }
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
            //string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            //string Agent = inputs.ContainsKey("Agent") && !string.IsNullOrEmpty(inputs["Agent"]) ? inputs["Agent"] : throw new Exception("Agent");
            //string Deskey = inputs.ContainsKey("Deskey") && !string.IsNullOrEmpty(inputs["Deskey"]) ? inputs["Deskey"] : throw new Exception("Deskey");
            //string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception("Md5key");

            //var current = new DateTimeOffset(DateTime.UtcNow);
            //var timestamp = current.ToUnixTimeMilliseconds();

            //var Param = new
            //{
            //    s = 4,
            //    orderid = $"{Agent}{TransID}"
            //};
            //var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            //var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var CheckTransfer = new
            //{
            //    agent = Agent,
            //    timestamp = timestamp,
            //    param = AesEncrypt(sParam, Deskey),
            //    key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            //};
            //dicP = FromJson<Dictionary<string, string>>(ToJson(CheckTransfer));
            //var sCheckTransfer = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var result = HttpGet(ApiUrl, sCheckTransfer);

            //ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            //if (IsJSON(result))
            //{
            //    var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            //    if (json.d.code != 0)
            //    {
            //        apiResult.RawMessage = ErrorMassage.ContainsKey(json.d.code) ? ErrorMassage[json.d.code] : json.d.code.ToString();
            //        apiResult.RawErrorCode = json.d.code.ToString();
            //        apiResult.ErrorCode = ErrorCode.TransferPending;
            //        apiResult.Data.Status = TransferStatus.Pending;
            //    }
            //    else
            //    {
            //        if (json.d.status == 0)
            //        {
            //            apiResult.ErrorCode = ErrorCode.Success;
            //            apiResult.Data.Status = TransferStatus.Success;
            //            apiResult.Data.ThirdId = TransID;
            //        }
            //        else if (json.d.status == 3)
            //        {
            //            apiResult.ErrorCode = ErrorCode.TransferPending;
            //            apiResult.Data.Status = TransferStatus.Pending;
            //            apiResult.Data.ThirdId = TransID;
            //        }
            //        else
            //        {
            //            apiResult.RawMessage = ErrorMassage.ContainsKey(json.d.code) ? ErrorMassage[json.d.code] : json.d.code.ToString();
            //            apiResult.RawErrorCode = json.d.code.ToString();
            //            apiResult.ErrorCode = ErrorCode.Fail;
            //            apiResult.Data.Status = TransferStatus.Fail;
            //        }
            //    }
            //}
            //else
            //{
            //    apiResult.RawMessage = "No Result";
            //    apiResult.RawErrorCode = "1";
            //    apiResult.ErrorCode = ErrorCode.TransferPending;
            //    apiResult.Data.Status = TransferStatus.Pending;
            //}

            //return apiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
            //string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            //string Agent = inputs.ContainsKey("Agent") && !string.IsNullOrEmpty(inputs["Agent"]) ? inputs["Agent"] : throw new Exception("Agent");
            //string Deskey = inputs.ContainsKey("Deskey") && !string.IsNullOrEmpty(inputs["Deskey"]) ? inputs["Deskey"] : throw new Exception("Deskey");
            //string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception("Md5key");

            //var current = new DateTimeOffset(DateTime.UtcNow);
            //var timestamp = current.ToUnixTimeMilliseconds();

            //var Param = new
            //{
            //    s = 7,
            //    account = tpCustInfo.TpUserName,
            //};
            //var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            //var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var Balance = new
            //{
            //    agent = Agent,
            //    timestamp = timestamp,
            //    param = AesEncrypt(sParam, Deskey),
            //    key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            //};
            //dicP = FromJson<Dictionary<string, string>>(ToJson(Balance));
            //var sBalance = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var result = HttpGet(ApiUrl, sBalance);

            //ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            //if (IsJSON(result))
            //{
            //    var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            //    if (json.d.code == 0)
            //    {
            //        oApiResult.ErrorCode = ErrorCode.Success;
            //        oApiResult.Data.balance = json.d.freeMoney; // 可下分余额
            //    }
            //    else
            //    {
            //        oApiResult.ErrorCode = ErrorCode.Fail;
            //        oApiResult.RawMessage = ErrorMassage.ContainsKey(json.d.code) ? ErrorMassage[json.d.code] : json.d.code.ToString();
            //        oApiResult.RawErrorCode = json.d.code.ToString();
            //    }
            //}
            //else
            //{
            //    oApiResult.RawMessage = "No Result";
            //    oApiResult.RawErrorCode = "1";
            //    oApiResult.ErrorCode = ErrorCode.Fail;
            //}
            //return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
            //TransIdModel transIdModel = new TransIdModel();
            //transIdModel.Implemented = true;
            //return transIdModel;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var tpConfig = _siteConfig[tpCustInfo.SiteId];
            string ApiUrl = tpConfig.ApiUrl;
            string Agent = tpConfig.CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            string Deskey = tpConfig.CurrencyData[tpCustInfo.TpCurrencyId].Deskey;
            string Md5key = tpConfig.CurrencyData[tpCustInfo.TpCurrencyId].Md5key;
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");

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
                timestamp = timestamp,
                param = AesEncrypt(sParam, Deskey),
                key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            };
            dicP = FromJson<Dictionary<string, string>>(ToJson(Login));

            var sLogin = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpGet(ApiUrl, sLogin);

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<ApiResponse>(result);
                if (json.d.code == 0)
                {
                    var uri = new Uri(json.d.url);
                    var forward = json.d.url;
                    string pLang = HttpUtility.ParseQueryString(uri.Query).Get("lang");
                    if (pLang != TpLangId)
                    {
                        forward = forward.Replace($"lang={pLang}", $"lang={TpLangId}");
                    }

                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = forward;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = ErrorMassage.ContainsKey(json.d.code) ? ErrorMassage[json.d.code] : json.d.code.ToString();
                    oApiResult.RawErrorCode = json.d.code.ToString();
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
            //string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            //string Agent = inputs.ContainsKey("Agent") && !string.IsNullOrEmpty(inputs["Agent"]) ? inputs["Agent"] : throw new Exception("Agent");
            //string Deskey = inputs.ContainsKey("Deskey") && !string.IsNullOrEmpty(inputs["Deskey"]) ? inputs["Deskey"] : throw new Exception("Deskey");
            //string Md5key = inputs.ContainsKey("Md5key") && !string.IsNullOrEmpty(inputs["Md5key"]) ? inputs["Md5key"] : throw new Exception("Md5key");

            //var current = new DateTimeOffset(DateTime.UtcNow);
            //var timestamp = current.ToUnixTimeMilliseconds();
            //var timecnv = current.ToString("yyyyMMddHHmmssfff");
            //int transType = (direction == Direction.ToMain) ? 3 : 2;

            //var Param = new
            //{
            //    s = transType,
            //    account = tpCustInfo.TpUserName,
            //    money = Amount,
            //    orderid = $"{Agent}{TransID}"
            //};
            //var dicP = FromJson<Dictionary<string, string>>(ToJson(Param));
            //var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var Transfer = new
            //{
            //    agent = Agent,
            //    timestamp = timestamp,
            //    param = AesEncrypt(sParam, Deskey),
            //    key = ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()
            //};
            //dicP = FromJson<Dictionary<string, string>>(ToJson(Transfer));
            //var sTransfer = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var result = HttpGet(ApiUrl, sTransfer);

            //ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            //apiResult.RawMessage = "No Result";
            //apiResult.RawErrorCode = "1";
            //apiResult.ErrorCode = ErrorCode.TransferPending;
            //apiResult.Data.Status = TransferStatus.Pending;
            //apiResult.Data.ThirdId = Param.orderid;

            //if (IsJSON(result))
            //{
            //    var json = JsonConvert.DeserializeObject<ApiResponse>(result);
            //    if (json.d.code != 0)
            //    {
            //        apiResult.RawMessage = ErrorMassage.ContainsKey(json.d.code) ? ErrorMassage[json.d.code] : json.d.code.ToString();
            //        apiResult.RawErrorCode = json.d.code.ToString() ?? "";
            //        if (json.d.code == 27)
            //        {
            //            // 数据库异常
            //            apiResult.ErrorCode = ErrorCode.TransferPending;
            //            apiResult.Data.Status = TransferStatus.Pending;
            //        }
            //        else
            //        {
            //            apiResult.ErrorCode = ErrorCode.Fail;
            //            apiResult.Data.Status = TransferStatus.Fail;
            //        }
            //    }
            //    else
            //    {
            //        apiResult.ErrorCode = ErrorCode.Success;
            //        apiResult.Data.Status = TransferStatus.Success;
            //        apiResult.RawMessage = "";
            //        apiResult.RawErrorCode = "";
            //    }
            //}
            //return apiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_V8_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
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
                TpCategory.Games => new PokerDetail(getBetDetail.TpGameTypeOption)
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_V8_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
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

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "8310" => "Fan Tan",
                "8290" => "Fish-Shrimp-Crab",
                "8200" => "Hundred Sic Bo",
                "890" => "Four Cards Bull",
                "910" => "Baccarat",
                "860" => "Three Toy",
                "600" => "Black Jack",
                "220" => "Three Cards",
                "830" => "Bull-Bull",
                "620" => "Texas Hold'em Poker",
                "8540" => "colored disc",
                "630" => "Thirteen Cards",
                "8700" => "Tien Len",
                "950" => "Red-Black War",
                "610" => "Fight the Landlord",
                "8600" => "Cat Te",
                "930" => "Hundreds People Of Bull",
                "8100" => "See Cards Bull",
                "510" => "Fishing Wars",
                "680" => "Fight the Landlord",
                "920" => "Forest Dance",
                "3101" => "See Cards Bull",
                "3890" => "Four Cards Bull",
                "8210" => "Tai Xiu",
                "8910" => "Cai Shen Dao",
                "1101" => "Ta La-Phom",
                "1102" => "Whot",
                _ => string.Empty,
            };
        }
    }
}
