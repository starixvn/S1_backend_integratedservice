using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.KY
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, KYConfig> _siteConfigDic;
        private readonly WebProxy? _proxy;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<KYConfig>(TpEnum.KY.ToString());

            var tokyoProxyConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ProxyConfig>("TokyoProxy");

            if (tokyoProxyConfig != null
                && !string.IsNullOrEmpty(tokyoProxyConfig.ProxyHost)
                && tokyoProxyConfig.ProxyPort > 0)
            {
                try
                {
                    _proxy = new WebProxy(tokyoProxyConfig.ProxyHost, tokyoProxyConfig.ProxyPort);
                }
                catch (UriFormatException ex)
                {
                    _proxy = null;
                }
            }
            else
            {
                _proxy = null;
            }
        }
        
        Dictionary<int, string> ErrorMassage = new Dictionary<int, string>()
        {   {-1,"上下分时，资料库异常回滚"},
            {0,"成功"},
            {1,"TOKEN 丢失"},
            {2,"渠道不存在"},
            {3,"验证时间超时"},
            {4,"验证错误"},
            {5,"渠道白名单错误"},
            {6,"验证字段丢失"},
            {7,"TOKEN 验证失败（重新调用登录界面）"},
            {8,"不存在的请求"},
            {11,"玩家账号不存在"},
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
            {40,"单次上下分数量不能超过一千万"},
            {41,"拉取对局汇总统计时间范围有误"},
            {42,"代理被禁用"},
            {43,"拉单过于频繁(两次拉单时间间隔，外测服必须大于10 秒，正式服必须大于5 秒)"},
            {44,"订单正在处理中"},
            {45,"参数错误"},
            {46,"时间范围错误"},
            {48,"游戏维护中"},
            {49,"语系参数错误"},
            {50,"币别参数错误"},
            {51,"超过单次最多账号查询数量"},
            {52,"玩家有异常盈利或分数异常状况时，呼叫API s=3 返回错误码52，当收到此响应再请洽询我方客服人员"},
            {89,"代理余额不足"},
            {91,"代理不存在"},
            {98,"進入遊戲發生错误"},
            {99,"系統非預期錯誤"},
        };

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];

            string ApiUrl = config.ApiUrl;
            string Agent = config.CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            string Md5key = config.CurrencyData[tpCustInfo.TpCurrencyId].Md5Key;
            string DesKey = config.CurrencyData[tpCustInfo.TpCurrencyId].DesKey;

            var currentAt = new DateTimeOffset(DateTime.UtcNow);
            var timestamp = currentAt.ToUnixTimeMilliseconds().ToString();
            var timecnv = currentAt.ToString("yyyyMMddHHmmssfff");

            string account = tpCustInfo.TpUserName;
            string orderId = $"{Agent}{timecnv}{account}";
            string ip = inputs.ContainsKey("ClientIP") && !string.IsNullOrEmpty(inputs["ClientIP"]) ? inputs["ClientIP"] : "";
            string lineCode = tpCustInfo.SiteId.ToString();
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");

            var parameters = new Dictionary<string, string>()
            {
                { "s", "0" },
                { "account", account },
                { "money", "0" },
                { "orderid", orderId },
                { "ip", ip },
                { "lineCode", lineCode },
                { "KindID", gameId }
            };

            var sParams = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));
            string eParams = AesEncrypt(sParams, DesKey);
            string postParams = $"agent={Agent}&timestamp={timestamp}&param={eParams}&key={ToMD5($"{Agent}{timestamp}{Md5key}").ToLower()}";

            var result = HttpGetWithProxy(ApiUrl, postParams, _proxy);

            ApiResult oApiResult = new ApiResult();
            if (string.IsNullOrEmpty(result) || !IsJSON(result))
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }
            else
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
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
            return oApiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new()
            {
                ErrorCode = ErrorCode.Success
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
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

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
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
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_KY_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_KY_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);
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
                "220" => "Golden Flower",
                "230" => "Speed Golden Flower",
                "510" => "Fishing",
                "520" => "LK Fishing",
                "530" => "ChinYuan Fishing",
                "540" => "BYCS Fishing",
                "550" => "Golden Dragon",
                "600" => "Black Jack",
                "610" => "Landlord",
                "620" => "Texas Hold'em Poker",
                "630" => "Pineapple Poker",
                "650" => "Bleeding Mahjong",
                "720" => "Two-Eight Bar",
                "730" => "Paigow",
                "740" => "2 Persons’ Mahjong",
                "830" => "Banker Bull-Bull",
                "840" => "Crazy Banker Bull-Bull",
                "860" => "Three-Facecard",
                "870" => "Casino Bull-Bull",
                "890" => "Banker Bull-Bull After Check Card",
                "900" => "Dragon-Tiger",
                "910" => "Baccarat",
                "920" => "Forest Party",
                "930" => "Niu–niu for thousands person",
                "931" => "Black Myth:Hundreds People of Bull",
                "950" => "Red&Black War",
                "1350" => "Lucky Dial",
                "1355" => "Give A Bet",
                "1370" => "Hong Kong Stud",
                "1602" => "Escape from Wuzhishan",
                "1610" => "Lucky Treasure",
                "1690" => "Karmic SicBo",
                "1810" => "1v1 Bull-Bull",
                "1850" => "Bet 3 Players Bull-Bull",
                "1860" => "Casino Poker",
                "1890" => "Fruit Machine",
                "1930" => "Fish-Prawn-Crab Dice",
                "1940" => "Gold Shark & Silver Shark",
                "1950" => "Golden Flower for thousands person",
                "1960" => "Benz & BMW",
                "1970" => "Five Stars",
                "1980" => "Sic Bo",
                "1990" => "Golden Bull",
                "2002" => "All-In or Fold",
                "2005" => "Three Card Poker",
                "2007" => "Lightning BlackJack",
                "2008" => "Ultimate Texas",
                "2011" => "GuanDan",
                "2012" => "TikTok Bull-Bull",
                "2890" => "Three-Facecard After Check Card",
                "3001" => "Speed Baccarat",
                "3002" => "World Cup Golden Ball",
                "3003" => "Ten Times Bull-Bull",
                "3005" => "European Roulette",
                "3006" => "H-baccarat",
                "7471" => "Mahjong Master 3",
                "7479" => "Bombing Fortune",
                "7484" => "Lucky Panda",
                "7485" => "Fortune FaFaFa",
                "7491" => "Monkey king : black wukong",
                "8130" => "Run Fast",
                _ => string.Empty,
            };
        }
    }
}
