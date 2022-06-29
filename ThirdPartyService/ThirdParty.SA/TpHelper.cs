using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private class CreateMemberReq
        {
            public string method { set; get; }
            public string key { set; get; }
            public string time { set; get; }
            public string username { set; get; }
            public string currencyType { set; get; }
        }

        //private class TransResult : Result
        //{
        //    public Data Data { get; set; }
        //}

        //private class Data
        //{
        //    public bool IsExist { get; set; }
        //    public string Username { get; set; }
        //    public bool IsSuccess { get; set; }
        //    public decimal Balance { get; set; }
        //    public bool Online { get; set; }
        //    public bool Betted { get; set; }
        //    public decimal BettedAmount { get; set; }
        //    public decimal MaxBalance { get; set; }
        //    public decimal MaxWinning { get; set; }
        //    public decimal WithholdAmount { get; set; }
        //    public string BetLimitList { get; set; }
        //    public string BetDetailList { get; set; }
        //    public string Token { get; set; }
        //    public string DisplayName { get; set; }
        //}

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }



        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            CreateMemberReq input = new CreateMemberReq();
            input.method = "RegUserInfo";

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x");

            input.username = tpCustInfo.TpUserName;
            input.currencyType = tpCustInfo.TpCurrencyId;
            var time = new DateTimeOffset(DateTime.Now);
            string _time = time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");
            input.time = _time;

            string secretKey = inputs.ContainsKey("SecretKey") && !string.IsNullOrEmpty(inputs["SecretKey"]) ? inputs["SecretKey"] : throw new Exception();
            string encrypKey = inputs.ContainsKey("EncrypKey") && !string.IsNullOrEmpty(inputs["EncrypKey"]) ? inputs["EncrypKey"] : throw new Exception();
            string md5Key = inputs.ContainsKey("MD5Key") && !string.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();

            string qs = $"method={input.method}&Key={secretKey}&Time={_time}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            string q = HttpUtility.UrlEncode(DESEncryptSA(qs, encrypKey));
            string s = ToMD5(qs + md5Key + _time + secretKey).ToLower();
            input.key = s;

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            //string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";
            string postdata = "q=" + q + "&s=" + s;

            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string langid, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();

            string urlKey = "GameUrl";
            string url = inputs.ContainsKey(urlKey) && !string.IsNullOrEmpty(inputs[urlKey]) ? inputs[urlKey] : throw new Exception("url Error");

            var time = new DateTimeOffset(DateTime.Now);
            var _time = time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");

            string secretKey = inputs.ContainsKey("SecretKey") && !String.IsNullOrEmpty(inputs["SecretKey"]) ? inputs["SecretKey"] : throw new Exception();
            string encrypKey = inputs.ContainsKey("EncrypKey") && !String.IsNullOrEmpty(inputs["EncrypKey"]) ? inputs["EncrypKey"] : throw new Exception();
            string md5Key = inputs.ContainsKey("MD5Key") && !String.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();
            string QS = $"method=LoginRequest&Key={secretKey}&Time={_time}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            string q = HttpUtility.UrlEncode(DESEncryptSA(QS, encrypKey));
            string s = ToMD5(QS + md5Key + _time + secretKey).ToLower();
            string postdata = "q=" + q + "&s=" + s;

            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");
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

                    string token = ((LoginResult)xml).Token;
                    string homeUrl = "";
                    string lobby = "A999";

                    if (inputs.ContainsKey("Lobby"))
                    {
                        lobby = inputs["Lobby"];
                    }

                    if (inputs.ContainsKey("homeUrl"))
                    {
                        homeUrl = inputs["homeUrl"];
                    }

                    if (appType == AppType.Native)
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.TpUserName}&token={token}&lobby={lobby}&lang={langid}&returnurl={homeUrl}&mobile=true";
                    }
                    else if (appType == AppType.Mobile)
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.TpUserName}&token={token}&lobby={lobby}&lang={langid}&returnurl={homeUrl}&mobile=true";
                    }
                    else
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.TpUserName}&token={token}&lobby={lobby}&lang={langid}&returnurl={homeUrl}&h5web=true";
                    }
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

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
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
            var transIdList = getBetDetail.TransId.Split("_");
            var wagerDetail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_SA_Detail_Get", new { GameID = transIdList[0], Username = transIdList[1] });
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
            var detail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_SA_Detail_Get", new { GameID = transIdList[0], Username = transIdList[1] });
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
    }
}
