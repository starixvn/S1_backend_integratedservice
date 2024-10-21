using System.Web;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SimplePlay
{
    public class TpHelper : ThirdPartyHelperBase
    {

        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, SimplePlayConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.SimplePlay).ToDictionary(d => d.AppId, d => d.Name);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SimplePlayConfig>(TpEnum.SimplePlay.ToString());
        }

        #region Response

        [XmlRoot("APIResponse")]
        public class Result
        {
            public int ErrorMsgId { get; set; }
            public string? ErrorMsg { get; set; }
        }

        [XmlRoot("LoginRequestResponse")]
        public class LoginResult : Result
        {
            public string? Token { set; get; }
            public string? DisplayName { set; get; }
            public string? GameURL { set; get; }
        }

        [XmlRoot("LoginRequestForFunResponse")]
        public class LoginFunResult : LoginResult
        {
        }

        [XmlRoot("GetSlotDetailsURLResponse")]
        public class GetDetailsURLResult : Result
        {
            public string? Url { set; get; }
        }

        #endregion

        private static string Time
        {
            get
            {
                var now = new DateTimeOffset(DateTime.Now);
                var time = now.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");
                return time;
            }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x");
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
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfigDic[tpCustInfo.SiteId];
            string gameId = inputs.GetValueOrDefault("gameId") ?? throw new Exception("gameId");
            string fun = inputs.GetValueOrDefault("fun") ?? "0";
            string homeUrl = inputs.GetValueOrDefault("homeUrl") ?? "";

            var time = Time;
            var mobile = (appType == AppType.Desktop) ? 0 : 1;
            string queryString;
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                queryString = $"method=LoginRequestForFun&Key={config.SecretKey}&Amount=10000&CurrencyType=IDR&Time={time}&GameCode={gameId}&Lang={TpLangId}&Mobile={mobile}";
            }
            else
            {
                queryString = $"method=LoginRequest&Key={config.SecretKey}&Time={time}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}&GameCode={gameId}&Lang={TpLangId}&Mobile={mobile}";
            }

            ApiResult oApiResult = new ApiResult();
            var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
            var s = ToMD5(queryString + config.MD5Key + time + config.SecretKey).ToLower();
            var postData = "q=" + q + "&s=" + s;
            var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            if (IsXML(result))
            {
                var xml = (fun.Equals("1", StringComparison.OrdinalIgnoreCase)) ? XMLDeserialize<LoginFunResult>(result) : XMLDeserialize<LoginResult>(result);

                if (xml.ErrorMsgId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{xml.GameURL}&returnurl={homeUrl}&skipintro=1";
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
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var time = Time;
            var method = "GetSlotDetailsURL";

            var split = getBetDetail.TransId.Split("_");
            var gameid = split[0];

            var queryString = $"method={method}&Key={config.SecretKey}&Time={time}&GameID={gameid}";
            var q = HttpUtility.UrlEncode(DESEncryptSA(queryString, config.EncryptKey));
            var s = ToMD5(queryString + config.MD5Key + time + config.SecretKey).ToLower();
            var postData = "q=" + q + "&s=" + s;

            var result = HttpPost(config.ApiUrl, postData, "application/x-www-form-urlencoded");
            if (!IsXML(result))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }

            var xml = XMLDeserialize<GetDetailsURLResult>(result);
            if (xml.ErrorMsgId != 0)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {xml.ErrorMsgId} | {xml.ErrorMsg}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = xml.Url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.TransTime).DateTime.GetPartitionKey();
            var wager = TpBetRecordProvider.DbQuery("Service_TpWagerRng_SimplePlay_Detail_Get", new { TransId = betRecordInfo.TransId }, DataBase.Wager);
            var bettrans = TpBetRecordProvider.DbQuery("Service_BettransRng_SimplePlay_Detail_Get", new { YYYYMM, betRecordInfo.TransId }, DataBase.Main);
            if (!bettrans.Any())
            {
                var YYYYMMCrossDate = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.TransTime).DateTime.AddDays(-1).GetPartitionKey();
                if (YYYYMM != YYYYMMCrossDate)
                {
                    bettrans = TpBetRecordProvider.DbQuery("Service_BettransRng_SimplePlay_Detail_Get", new { YYYYMMCrossDate, betRecordInfo.TransId }, DataBase.Main);
                }
            }
            if (!bettrans.Any() || !wager.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = bettrans.GetFirstStringValue("gamecode");
            var round = bettrans.GetFirstStringValue("gameid");
            var ip = bettrans.GetFirstStringValue("ip");
            var platform = bettrans.GetFirstStringValue("platform");
            var betId = wager.GetFirstStringValue("BetID");
            var betSource = wager.GetFirstStringValue("BetSource");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = round,
                Title = tpGameType.TypeName,
                MatchID = betId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(platform, betSource),
                IP = ip,
            };
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return (dic != null) ? dic.TryGetValue("en", out var result) ? result : string.Empty : string.Empty;
        }

        private static DeviceType GetDeviceType(string platform, string betSource)
        {
            DeviceType deviceType;
            switch (betSource)
            {
                case "2":
                case "2114":
                case "2640":
                    deviceType = DeviceType.Desktop;
                    break;
                case "16":
                case "600":
                case "8792":
                case "8793":
                    deviceType = DeviceType.Mobile;
                    break;
                case "1160":
                case "1161":
                case "5248":
                case "5249":
                    deviceType = DeviceType.iOS;
                    break;
                case "1288":
                case "1289":
                case "5376":
                case "5377":
                    deviceType = DeviceType.Android;
                    break;
                default:
                    deviceType = (platform == "1") ? DeviceType.Mobile : DeviceType.Desktop;
                    break;
            }

            return deviceType;

        }
    }
}
