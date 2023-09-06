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

        private string GetBetLimitQueryString(string tpCurrencyId, Dictionary<string, string> inputs)
        {
            //用第三方提供的文件設定好RuleID
            var dictMapInputKey = new Dictionary<string, string>()
            {
                {"MYR", "10104581000110003,10204581000110003,10304581000110003,10504581000110003,11004581000110003,11504581000110003,11604581000110003,11704581000110003,10104581000210004,10204581000210004,10304581000210004,10504581000210004,11004581000210004,11504581000210004"},
                {"IDR", "10103601000110003,10203601000110003,10303601000110003,10503601000110003,11003601000110003,11503601000110003,11603601000110003,11703601000110003,10103601000210004,10203601000210004,10303601000210004,10503601000210004,11003601000210004,11503601000210004,11603601000210004,11703601000210004,10103603000230004,10203603000230004,10303603000230004,10503603000230004,11003603000230004,11503603000230004,11603603000230004,11703603000230004"},
                {"VND", "10107042000110004,10207042000110004,10307042000110004,10507042000110004,11007042000110004,11507042000110004,11607042000110004,11707042000110004,10107043000230004,10207043000230004,10307043000230004,10507043000230004,11007043000230004,11507043000230004,11607043000230004,11707043000230004,10107041000350004,10207041000350004,10307041000350004,10507041000350004,11007041000350004,11507041000350004,11607041000350004,11707041000350004"},
                {"USD", "10108401000025003,10208401000025003,10308401000025003,10508401000025003,11008401000025003,11508401000025003,11608401000025003,11708401000025003"},
                {"INR", "10103561000210005,10203561000210005,10303561000210005,10503561000210005,11003561000210005,11503561000210005,11603561000210005,11703561000210005,10103561000320005,10203561000320005,10303561000320005,10503561000320005,11003561000320005,11503561000320005,11603561000320005,11703561000320005"},
                {"BDT", "10100501000210004,10200501000210004,10300501000210004,10500501000210004,11000501000210004,11500501000210004,11600501000210004,11700501000210004,10100503000230004,10200503000230004,10300503000230004,10500503000230004,11000503000230004,11500503000230004,11600503000230004,11700503000230004,10100501000320005,10200501000320005,10300501000320005,10500501000320005,11000501000320005,11500501000320005,11600501000320005,11700501000320005"}
            };

            if (!dictMapInputKey.TryGetValue(tpCurrencyId, out var betLimitKeys)) return string.Empty;

            return $"ListSet={betLimitKeys}";
        }

        /// <summary>
        /// 更新使用者 BetLimit
        /// </summary>
        /// <param name="config"></param>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        private void SetBetLimit(SaConfig config, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                var betLimitQueryString = GetBetLimitQueryString(tpCustInfo.TpCurrencyId, inputs);
                if (string.IsNullOrEmpty(betLimitQueryString)) return;

                var time = this.Time;
                var input = new SetBetLimitReq()
                {
                    Method = "SetAllBetLimit",
                    Key = config.SecretKey,
                    Time = time,
                    UserName = tpCustInfo.TpUserName,
                    Currency = tpCustInfo.TpCurrencyId
                };

                var queryString =
                    $"method={input.Method}&Key={input.Key}&Time={time}&Username={input.UserName}&Currency={input.Currency}&{betLimitQueryString}";
                var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
                var s = ToMD5(queryString + config.Md5Key + time + config.SecretKey).ToLower();
                var postData = "q=" + q + "&s=" + s;

                var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            }
            catch (Exception ex)
            {
                //Exception 暫不處理，設定 BetLimit 失敗不該影響玩家正常進入遊戲
            }
        }

        #endregion
    }
}
