using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SA
{
    public class TpHelper : ThirdPartyHelperBase
    {

        [XmlRoot("APIResponse")]
        public class Result
        {
            public int ErrorMsgId { get; set; }
            public string ErrorMsg { get; set; }
        }

        [XmlRoot("RegUserInfoResponse")]
        public class RegUserInfoResponse : Result
        {
            public string Username { get; set; }
        }

        [XmlRoot("LoginRequestResponse")]
        public class LoginResult : Result
        {
            public string Token { set; get; }
            public string DisplayName { set; get; }
        }

        public class BaseRequest
        {
            public string Method { set; get; }
            public string Key { set; get; }
            public string Time { set; get; }
            public string UserName { set; get; }
            public string Currency { set; get; }
        }

        private class CreateMemberReq : BaseRequest { }

        #region QueryBetLimit

        public class QueryBetLimitReq : BaseRequest { }

        [XmlRoot("QueryBetLimitResponse")]
        public class QueryBetLimitResult : Result
        {
            [XmlElement(ElementName = "BetLimitList")]
            public BetLimitList BetLimitList { get; set; }
        }


        [XmlRoot(ElementName = "BetLimitList")]
        public class BetLimitList
        {
            [XmlElement(ElementName = "BetLimit")]
            public List<BetLimit> BetLimit { get; set; }
        }

        [XmlRoot(ElementName = "BetLimit")]
        public class BetLimit
        {
            [XmlElement(ElementName = "RuleID")]
            public string RuleID { get; set; }

            [XmlElement(ElementName = "Min")]
            public decimal Min { get; set; }

            [XmlElement(ElementName = "Max")]
            public decimal Max { get; set; }
        }

        #endregion

        #region SaConfig

        private class SaConfig
        {
            public string ApiUrl { get; set; }

            public string SecretKey { get; set; }

            public string EncryptKey { get; set; }

            public string Md5Key { get; set; }

            public string Url { get; set; }

            public SaConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
                SecretKey = inputs.ContainsKey("SecretKey") && !string.IsNullOrEmpty(inputs["SecretKey"]) ? inputs["SecretKey"] : throw new Exception();
                EncryptKey = inputs.ContainsKey("EncrypKey") && !string.IsNullOrEmpty(inputs["EncrypKey"]) ? inputs["EncrypKey"] : throw new Exception();
                Md5Key = inputs.ContainsKey("MD5Key") && !string.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();
                Url = inputs.ContainsKey("GameUrl") && !string.IsNullOrEmpty(inputs["GameUrl"])
                    ? inputs["GameUrl"]
                    : string.Empty;
            }
        }

        #endregion

        #region SetBetLimit 

        public class SetBetLimitReq : BaseRequest { }

        #endregion

        private string Time
        {
            get
            {
                var now = new DateTimeOffset(DateTime.Now);
                var time = now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");
                return time;
            }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new SaConfig(inputs);

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x");
            var input = new CreateMemberReq
            {
                Method = "RegUserInfo",
                UserName = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId
            };

            var time = Time;
            input.Time = time;

            var queryString = $"method={input.Method}&Key={config.SecretKey}&Time={time}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
            var s = ToMD5(queryString + config.Md5Key + time + config.SecretKey).ToLower();
            input.Key = s;

            var postData = "q=" + q + "&s=" + s;
            var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsXML(result))
            {

                Result xml = null;
                try
                {
                    xml = XMLDeserialize<RegUserInfoResponse>(result);
                }
                catch (Exception)
                {
                    xml = XMLDeserialize<Result>(result);
                }

                if (xml.ErrorMsgId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = xml.ErrorMsg;
                    oApiResult.RawErrorCode = xml.ErrorMsgId.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string langid, Dictionary<string, string> inputs)
        {
            var config = new SaConfig(inputs);
            if (string.IsNullOrEmpty(config.Url)) throw new Exception("url Error");

            SetBetLimit(config, tpCustInfo, inputs);

            var time = Time;
            var queryString = $"method=LoginRequest&Key={config.SecretKey}&Time={time}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
            var s = ToMD5(queryString + config.Md5Key + time + config.SecretKey).ToLower();
            var postData = "q=" + q + "&s=" + s;

            var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsXML(result))
            {
                Result xml = null;

                try
                {
                    xml = XMLDeserialize<LoginResult>(result);
                }
                catch (Exception)
                {
                    xml = XMLDeserialize<Result>(result);
                }

                if (xml.ErrorMsgId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;

                    var token = ((LoginResult)xml).Token;
                    var homeUrl = "";
                    var lobby = "A999";

                    if (inputs.ContainsKey("Lobby"))
                    {
                        lobby = inputs["Lobby"];
                    }

                    if (inputs.ContainsKey("homeUrl"))
                    {
                        homeUrl = inputs["homeUrl"];
                    }

                    var deviceName = GetDeviceName(appType);
                    oApiResult.Message = $"{config.Url}?username={tpCustInfo.TpUserName}&token={token}&lobby={lobby}&lang={langid}&returnurl={homeUrl}&{deviceName}=true";
                }
                else
                {
                    oApiResult.RawMessage = xml.ErrorMsg;
                    oApiResult.RawErrorCode = xml.ErrorMsgId.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var transIdList = getBetDetail.TransId.Split("_");
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_SA_Detail_Get", new { GameID = transIdList[0], Username = transIdList[1] }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            var gameCode = wagerDetail.GetStringValue("GameType");
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("GameID")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("Username")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("BetTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("PayoutTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("BetAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("ResultAmount") * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
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
            var transIdList = betRecordInfo.TransId.Split("_");
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_SA_Detail_Get", new { GameID = transIdList[0], Username = transIdList[1] }, DataBase.Wager);
            var gameCode = detail.GetStringValue("GameType");

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = detail.GetStringValue("GameID"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("Round"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("GameResult"),
                    DeviceType = GetDeviceType(detail.GetStringValue("BetSource")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        #region 未實作

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

        #endregion

        #region private method

        private DeviceType GetDeviceType(string device)
        {
            switch (device)
            {
                case "2":
                case "2114":
                case "2640":
                case "3":
                case "2115":
                case "2641":
                    return DeviceType.Desktop;
                case "16":
                case "600":
                case "8792":
                case "8793":
                    return DeviceType.Mobile;
                case "1160":
                case "1161":
                case "5248":
                case "5249":
                    return DeviceType.iOS;
                case "1288":
                case "1289":
                case "5376":
                case "5377":
                    return DeviceType.Android;
                default:
                    return DeviceType.None;
            }
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "bac" => "Baccarat",
                "dtx" => "Dragon Tiger",
                "sicbo" => "Sic Bo",
                "rot" => "Roulette",
                "pokdeng" => "Pok Deng",
                _ => gameCode,
            };
        }

        private string GetDeviceName(AppType appType)
        {
            string deviceName;
            switch (appType)
            {
                case AppType.Native:
                case AppType.Mobile:
                    deviceName = "mobile";
                    break;
                default:

                    deviceName = "h5web";
                    break;
            }

            return deviceName;
        }

        /// <summary>
        /// 依據幣別取得第三方個幣別提供的 BetLimit RuleID，有需要更新在呼叫Api取就好，dev、prd Local都會通，更改config即可
        /// </summary>
        /// <param name="config"></param>
        private void QueryBetLimit(SaConfig config)
        {
            var time = this.Time;
            var saCurrency = new List<string>()
            {
                "MYR", "THB", "IDR",  "VND", "USD", "INR", "BDT"
            };

            foreach (var currency in saCurrency)
            {
                var input = new QueryBetLimitReq()
                {
                    Method = "QueryBetLimit",
                    Key = config.SecretKey,
                    Time = time,
                    Currency = currency
                };

                var queryString = $"method={input.Method}&Key={input.Key}&Time={time}&Currency={input.Currency}";
                var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
                var s = ToMD5(queryString + config.Md5Key + time + config.SecretKey).ToLower();
                var postData = "q=" + q + "&s=" + s;

                var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");

                QueryBetLimitResult queryBetLimitResult = null;
                if (IsXML(result))
                {
                    queryBetLimitResult = XMLDeserialize<QueryBetLimitResult>(result);
                }

                var firstRuleId = string.Empty; // 第一段級距
                var secondRuleId = string.Empty; // 第二段級距
                var threeRuleId = string.Empty; // 第三段級距

                switch (input.Currency)
                {
                    case "MYR":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 1 && r.Max == 10000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 300 && r.Max == 30000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "THB":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 5 && r.Max == 75000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 2000 && r.Max == 200000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "IDR":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 1 && r.Max == 30000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 1000 && r.Max == 100000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "VND":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 2 && r.Max == 50000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 2000 && r.Max == 200000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "USD":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 1 && r.Max == 2500).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 100 && r.Max == 10000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "INR":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 100 && r.Max == 100000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 700 && r.Max == 200000).Select(r => r.RuleID).FirstOrDefault();
                        threeRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 2500 && r.Max == 750000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                    case "BDT":
                        firstRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 100 && r.Max == 10000).Select(r => r.RuleID).FirstOrDefault();
                        secondRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 500 && r.Max == 100000).Select(r => r.RuleID).FirstOrDefault();
                        threeRuleId = queryBetLimitResult.BetLimitList.BetLimit.Where(r => r.Min == 2500 && r.Max == 750000).Select(r => r.RuleID).FirstOrDefault();
                        break;
                }

                var betLimitInfo = $"{input.Currency}：fristRuleID：{firstRuleId},secondRuleID:{secondRuleId},threeRuleId:{threeRuleId}";
                // ex：MYR：fristRuleID：17592186044416,secondRuleID:524288,threeRuleId:
            }
        }

        /// <summary>
        /// 用 tpCurrencyId ，找出 db 內有的 BetLimit Key
        /// </summary>
        /// <param name="tpCurrencyId"></param>
        /// <returns></returns>
        private string GetBetLimitQueryString(string tpCurrencyId, Dictionary<string, string> inputs)
        {
            var dictMapInputKey = new Dictionary<string, List<string>>()
            {
                {"MYR", new List<string>() { "MYRBetLimit1", "MYRBetLimit2" }},
                {"THB", new List<string>() { "THBBetLimit1", "THBBetLimit2" }},
                {"IDR", new List<string>() { "IDRBetLimit1", "IDRBetLimit2" }},
                {"VND", new List<string>() { "VNDBetLimit1", "VNDBetLimit2" }},
                {"USD", new List<string>() { "USDBetLimit1", "USDBetLimit2" }},
                {"INR", new List<string>() { "INRBetLimit1", "INRBetLimit2","INRBetLimit3" }},
                {"BDT", new List<string>() { "BDTBetLimit1", "BDTBetLimit2","BDTBetLimit3" }}
            };

            if (!dictMapInputKey.TryGetValue(tpCurrencyId, out var betLimitKeys)) return string.Empty;

            var queryStringList = new List<string>();
            foreach (var (value, index) in betLimitKeys.Select((value, index) => (value, index)))
            {
                queryStringList.Add($"Set{index + 1}={inputs[value]}");
            }

            return string.Join("&", queryStringList);
        }

        /// <summary>
        /// 更新使用者 BetLimit
        /// </summary>
        /// <param name="config"></param>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        private void SetBetLimit(SaConfig config, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {

            var betLimitQueryString = GetBetLimitQueryString(tpCustInfo.TpCurrencyId, inputs);
            if (string.IsNullOrEmpty(betLimitQueryString)) return;

            var time = this.Time;
            var input = new SetBetLimitReq()
            {
                Method = "SetBetLimit",
                Key = config.SecretKey,
                Time = time,
                UserName = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId
            };


            var queryString =
                $"method={input.Method}&Key={input.Key}&Time={time}&Username={input.UserName}&Currency={input.Currency}&{betLimitQueryString}&Gametype=roulette,sicbo,pokdeng,others";
            var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
            var s = ToMD5(queryString + config.Md5Key + time + config.SecretKey).ToLower();
            var postData = "q=" + q + "&s=" + s;

            var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            throw new Exception(result);
        }

        #endregion
    }
}
