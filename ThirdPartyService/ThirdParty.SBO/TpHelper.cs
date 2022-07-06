using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SBO
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private const string ContentType = "application/json";

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new SboConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/web-root/restricted/player/register-player.aspx";

            var registerPlayer = new
            {
                Username = tpCustInfo.TpUserName,
                Agent = config.Agent,
                CompanyKey = config.CompanyKey,
                ServerId = config.ServerId
            };

            var responseJson = HttpPostClient(apiUrl,
                JsonConvert.SerializeObject(registerPlayer),
                ContentType);

            var responseResult = FromJson<RegisterPlayerResponse>(responseJson);
            if (responseResult.Error.Id == 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawMessage = responseResult.Error.Msg,
                RawErrorCode = responseResult.Error.Id.ToString()
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = new SboConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/web-root/restricted/player/login.aspx";

            var playerLogin = new
            {
                Username = tpCustInfo.TpUserName,
                Portfolio = TpGameTypeMap(int.Parse(config.TpGameTypeId)),
                CompanyKey = config.CompanyKey,
                ServerId = config.ServerId,
            };

            var responseJson = HttpPostClient(apiUrl,
                JsonConvert.SerializeObject(playerLogin),
                ContentType);

            var responseResult = FromJson<PlayerLoginResponse>(responseJson);
            if (responseResult.Error.Id != 0)
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawMessage = responseResult.Error.Msg,
                    RawErrorCode = responseResult.Error.Id.ToString()
                };

            var device = (appType == AppType.Desktop ? "d" : "m");
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"https:{responseResult.LoginUrl}&lang={TpLangId}&oddstyle=MY&theme=sbo&oddsmode=double&device={device}"
            };
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };


            var betDetail = getBetDetail.TpGameTypeOption.Type == (int)TpGameType.SBO_Sports
                ? GetSportsBookDetail(getBetDetail, inputs, tpCurrency) // SportsBook(打第三方API取得)
                : GetVirtualSportsDetail(getBetDetail, tpCurrency);  // VirtualSports(查TpWager)

            return betDetail;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            if (tpGameType.CategoryId == TpCategory.Sports || tpGameType.CategoryId == TpCategory.VirtualSports)
            {
                var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerSBO_Get", new { betRecordInfo.TransId }, DataBase.Wager);
                var betList = JsonConvert.DeserializeObject<IEnumerable<subBet>>(detail.GetStringValue("subBet"));
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("sportsType"),
                    Game = detail.GetStringValue("sportsType"),
                    RefNo = detail.GetStringValue("refNo"),
                    Title = betList.FirstOrDefault()?.match ?? tpGameType.TypeName,
                    MatchID = string.Empty,
                    BetType = betList.FirstOrDefault()?.marketType ?? string.Empty,
                    BetChoice = betList.FirstOrDefault()?.betOption ?? string.Empty,
                    Result = detail.GetStringValue("status"),
                    Odds = decimal.TryParse(detail.GetStringValue("odds"), out var odds) ? odds : 0,
                    OddsTypeEnum = GetOddsType(detail.GetStringValue("oddsStyle")),
                    DeviceType = DeviceType.None,
                    Live = detail.GetStringValue("isLive"),
                    IP = detail.GetStringValue("ip"),
                };
            }
            else
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }      
        }

        #region 單一錢包不實作

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }


        #endregion

        private class SboConfig
        {
            public string ApiUrl { get; private set; }

            public string CompanyKey { get; private set; }

            public string ServerId { get; private set; }

            public string Agent { get; private set; }

            /// <summary>
            /// 遊戲類型，只有Login的時候前端會帶入
            /// </summary>
            public string TpGameTypeId { get; private set; }

            public SboConfig(Dictionary<string, string> config)
            {
                ApiUrl = config.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(config["ApiUrl"])
                    ? config["ApiUrl"]
                    : throw new Exception("ApiUrl is empty");

                CompanyKey = config.ContainsKey("CompanyKey") && !string.IsNullOrEmpty(config["CompanyKey"])
                    ? config["CompanyKey"]
                    : throw new Exception("CompanyKey is empty");

                ServerId = config.ContainsKey("ServerId") && !string.IsNullOrEmpty(config["ServerId"])
                    ? config["ServerId"]
                    : throw new Exception("ServerId is empty");

                Agent = config.ContainsKey("Agent") && !string.IsNullOrEmpty(config["Agent"])
                    ? config["Agent"]
                    : throw new Exception("Agent is empty");

                TpGameTypeId = config.ContainsKey("tpGameType") ? config["tpGameType"] : string.Empty;
            }
        }

        private class ResponseError
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("msg")]
            public string Msg { get; set; }
        }

        private class RegisterPlayerResponse
        {
            [JsonProperty("serverId")]
            public string ServerId { get; set; }

            [JsonProperty("error")]
            public ResponseError Error { get; set; }
        }

        private class PlayerLoginResponse
        {
            [JsonProperty("url")]
            public string LoginUrl { get; set; }

            [JsonProperty("serverId")]
            public string ServerId { get; set; }

            [JsonProperty("error")]
            public ResponseError Error { get; set; }

        }

        private class BetDetailResponse
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("serverId")]
            public string ServerId { get; set; }

            [JsonProperty("error")]
            public ResponseError Error { get; set; }
        }

        private static string TpGameTypeMap(int gameTypeId)
        {
            #region 所有分類，未來可擴充

            // SportsBook
            // Casino
            // Games
            // VirtualSports
            // SeamlessGame
            // ThirdPartySportsBook
            // Sbo568Sport

            #endregion

            var gameTypeDict = new Dictionary<int, string>()
            {
                {(int)TpGameType.SBO_Sports,"SportsBook"},
                {(int)TpGameType.SBO_VirtualSports,"VirtualSports"}
            };

            return gameTypeDict[gameTypeId];
        }

        /// <summary>
        /// 取得住單詳情，GameType = SportsBook 打第三方API要資料
        /// </summary>
        /// <param name="getBetDetail"></param>
        /// <param name="inputs"></param>
        /// <param name="tpCurrency"></param>
        /// <returns></returns>
        /// <exception cref="BetDetailNotFoundException"></exception>
        private BaseDetail GetSportsBookDetail(GetBetDetail getBetDetail,
            Dictionary<string, string> inputs,
            TpCurrency tpCurrency)
        {

            var config = new SboConfig(inputs);
            var apiUrl = $"{config.ApiUrl}/web-root/restricted/report/get-bet-payload.aspx";

            var betDetail = new
            {
                CompanyKey = config.CompanyKey,
                ServerId = config.ServerId,
                Portfolio = "SportsBook",
                Refno = getBetDetail.TransId
            };

            var responseJson = HttpPostClient(apiUrl,
                JsonConvert.SerializeObject(betDetail),
                ContentType);

            var responseResult = FromJson<BetDetailResponse>(responseJson);

            if (responseResult.Error.Id == 0)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = responseResult.Url,
                    TpCurrency = tpCurrency
                };
            }

            throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {responseResult.Error.Msg}");
        }

        /// <summary>
        /// 取得住單詳情，GameType = VirtualSports 去查 TpWager
        /// </summary>
        /// <param name="getBetDetail"></param>
        /// <param name="tpCurrency"></param>
        /// <returns></returns>
        private SportsDetail GetVirtualSportsDetail(GetBetDetail getBetDetail, TpCurrency tpCurrency)
        {
            const string spName = "Service_TpWagerSBO_Get";
            var wager = TpBetRecordProvider.DbQueryFirst(spName, new { getBetDetail.TransId }, DataBase.Wager);
            var result = new SportsDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = wager.GetValueOrDefault("wagerDetail")?.ToString(),
                TpCurrency = tpCurrency,
                TpUserName = wager.GetValueOrDefault("TpUserName")?.ToString(),
                UTCBetTime = ConvertToUtcTime(wager.GetValueOrDefault("BetTime")?.ToString()),
                UTCSettledTime = ConvertToUtcTime(wager.GetValueOrDefault("SettledTime")?.ToString()),
                Stake = (decimal)wager.GetValueOrDefault("Stake"),
                WinlostAmount = (decimal)wager.GetValueOrDefault("WinlostAmount"),
                Odds = wager.GetValueOrDefault("Odds")?.ToString()
            };

            return result;
        }

        /// <summary>
        /// 時區轉換 SBO 存 -4 時區，User要看 +0 時區
        /// </summary>
        /// <param name="dbDateTime"></param>
        /// <returns></returns>
        private DateTime ConvertToUtcTime(string dbDateTime)
        {
            var dateTime = DateTime.Parse(dbDateTime);
            var dateTimeOffset = new DateTimeOffset(dateTime, new TimeSpan(-4, 0, 0));
            return dateTimeOffset.ToOffset(new TimeSpan(0, 0, 0)).DateTime;
        }

        #region ApiErrorCode對應

        // Value     描述
        // 0	    No Error
        // 1	    无效的Company Key
        // 2	    无效的请求格式
        // 3	    内部错误
        // 4	    无效的会员名称
        // 5	    无效的国家
        // 6	    无效的语言
        // 13	    请选择安全系数更高的账号。建议您使用字母、数字和 '_' 的组合
        // 3101	    无效的币别
        // 3102	    无效的主题Id
        // 3104	    创建代理失败
        // 3201	    更新状态失败
        // 3202	    无效的会员名称-更新状态
        // 3203	    已更新状态
        // 3204	    无效的状态
        // 3205	    无效的日期
        // 3206	    无效的单笔注单最低限额
        // 3207	    无效的单笔注单最高限额
        // 3208	    无效的单场比赛最高限额
        // 3209	    无效的真人赌场下注设定
        // 3303	    用户不存在
        // 4101	    代理不存在
        // 4102	    创建会员失败
        // 4103	    会员名称存在
        // 4106	    代理帐号为关闭状态
        // 4107	    請創建在代理而非下线底下
        // 4201	    验证失败
        // 4401	    无效的交易Id
        // 4402	    无效的交易金额.
        //          比如:输入金额为 负值 或
        //          输入金额含有 超过两位小数(范例: 19.217 和 19.2245 均会报错)
        // 4403	    交易失败
        // 4404	    重复使用相同的交易Id
        // 4501	    余额不足
        // 4502	    于余额不足导致的回滚(Rollback Transaction)交易
        // 4601	    检查交易状态失败
        // 4602	    未找到任何交易
        // 4701	    获得余额失败
        // 6101	    获取客户报表失败
        // 6102	    获取客户注单失败
        // 6666	    没有此下注纪录
        // 9527	    无效的运动类型
        // 9528	    无效的盘口
        // 9720	    提款请求次数太过频繁
        // 9721	    无效的密码格式

        #endregion

        private class subBet
        {         
            public string match { get; set; }       
            public string betOption { get; set; }
            public string marketType { get; set; }
        }

        private OddsTypeEnum GetOddsType(string oddsType)
        {
            return oddsType switch
            {
                "M" => OddsTypeEnum.MalayOdds,
                "H" => OddsTypeEnum.HongKongOdds,
                "E" => OddsTypeEnum.DecimalOdds,
                "I" => OddsTypeEnum.IndoOdds,
                _ => OddsTypeEnum.None,
            };
        }

    }
}
