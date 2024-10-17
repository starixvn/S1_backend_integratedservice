using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.EBet
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, EBetConfig> _siteEBetConfig;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteEBetConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<EBetConfig>(TpEnum.EBet.ToString());
        }
        
        #region Model
        
        public class DetailResult
        {
            public int status { get; set; }
            public string path { get; set; }
        }

        public class GetLaunchUrlResponse
        {
            public int status { get; set; }
            public string launchUrl { get; set; }
            public string apiVersion { get; set; }
        }
        
        public class EBetConfig
        {
            public string ApiUrl { get; set; }
            public List<Currency> Currency { get; set; }
        }
        
        public class Currency
        {
            public string CurrencyId { get; set; }
            public string GameUrl { get; set; }
            public int ChannelId { get; set; }
            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }
        
        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
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
            var currency = tpCustInfo.TpCurrencyId;
            var currencyConfig = GetCurrencyConfig(tpCustInfo.SiteId, currency);
            if (currencyConfig == null)
                throw new Exception($"CurrencyId {currency} is not found");
            var config = _siteEBetConfig[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var channelId = currencyConfig.ChannelId.ToString();
            var privateKey = currencyConfig.PrivateKey;
            var clientIp = inputs["ClientIP"];
            var category = "Live";
            var uiMode = appType != AppType.Desktop ? "mobile" : "auto";
            
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrWhiteSpace(inputs["fun"]) ? inputs["fun"] : "0";
            var isDemo = fun.Equals("1", StringComparison.OrdinalIgnoreCase); 
            
            var key = ApplyToken(tpCustInfo);
            var sessionToken = HttpUtility.UrlEncode(key);
            
            var domainUrl = GetUrlForLogin(tpCustInfo.TpUserName, apiUrl, channelId, clientIp,isDemo, sessionToken ,TpLangId, category, uiMode, privateKey, out var launchUrl)
                ? launchUrl
                : string.Empty;

            if (string.IsNullOrWhiteSpace(domainUrl))
            {
                //launchUrl沒有拿到資料，則用db的GameUrl
                domainUrl = currencyConfig.GameUrl;
            }
            
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = domainUrl
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var currencyConfig = GetCurrencyConfig(tpCustInfo.SiteId, tpCustInfo.TpCurrencyId);
            if (currencyConfig == null)
                throw new Exception($"CurrencyId {tpCustInfo.TpCurrencyId} is not found");
            var currency = tpCustInfo.TpCurrencyId;
            var config = _siteEBetConfig[tpCustInfo.SiteId];
            var apiUrl = config.ApiUrl;
            var channelId = currencyConfig.ChannelId.ToString();
            var privateKey = currencyConfig.PrivateKey;
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransLC_EBet_Detail_Get", new { getBetDetail.TransId }, DataBase.Report);

            var roundCode = detail.GetValueOrDefault("roundCode").ToString();
            var username = detail.GetValueOrDefault("username").ToString();
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var signature = RSA($"{username}{timestamp}", privateKey);
            var lang = "en_us";
            var sParam = new
            {
                channelId,
                timestamp,
                signature,
                username,
                roundCode,
                currency,
                lang
            };

            var url = $"{apiUrl}/api/userBetTracer";
            var json = HttpPost(url, ToJson(sParam), "application/json");
            if (!IsJSON(json))
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail error. {json}");
            }

            var result = FromJson<DetailResult>(json);
            if (result.status != 200)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = $"{result.path}&signature={RSA($"{channelId}{username}{roundCode}{lang}", privateKey)}",
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransLC_EBet_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
            var ebet = JsonConvert.DeserializeObject<DetailEbet>(detail.GetStringValue("detail"));
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = ebet.tableType.ToString(),
                    Game = GetGameName(ebet.tableType.ToString()),
                    RefNo = detail.GetStringValue("seqNo"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.GetStringValue("roundCode"),
                    BetType = ebet.betList?.First()?.betType.ToString() ?? string.Empty,
                    BetChoice = string.Empty,
                    Result = detail.GetStringValue("results"),
                    DeviceType = GetDeviceType(detail.GetStringValue("platform")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "3" => DeviceType.Desktop,
                "1" => DeviceType.iOS,
                "0" => DeviceType.Android,
                "2" => DeviceType.Mobile,
                _ => DeviceType.None,
            };
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "0" => "WEH Live",
                "1" => "Baccarat",
                "2" => "Dragon Tiger",
                "3" => "Roulette",
                "4" => "Sic Bo(Dice)",
                "5" => "Slot",
                "8" => "BullBull",
                "9" => "Win Three Cards",
                "10" => "GOF Baccarat",
                "11" => "GOF Roulette",
                "12" => "GOF Sic-bo",
                "13" => "FanTan",
                "23" => "Fortune Wheel",
                "24" => "Black Jack",
                "25" => "Live Black Jack",
                "29" => "Pok Deng",
                _ => gameCode,
            };
        }

        private Currency GetCurrencyConfig(int siteId ,string currencyId)
        {
            var config = _siteEBetConfig[siteId];
            return config.Currency.FirstOrDefault(x => x.CurrencyId == currencyId);
        }

        #region Login Step

        private bool GetUrlForLogin(string userName, string apiUrl, string channelId, string clientIP, bool isDemo, string sessionToken, string language, string category, string uiMode ,string privateKey, out string launchUrl)
        {
            launchUrl = string.Empty;
            try
            {
                var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                var signature = RSA($"{userName}{timestamp}", privateKey);

                var param = new
                {
                    channelId,
                    timestamp,
                    signature,
                    clientIP,
                    isDemo,
                    userName,
                    sessionToken,
                    language,
                    category,
                    uiMode
                };
                
                var url = $"{apiUrl}/api/playerlaunch";
                var result = HttpPost(url, ToJson(param), "application/json");

                if (!IsJSON(result))
                    return false;

                var userResult = FromJson<GetLaunchUrlResponse>(result);

                if (userResult.status != 200) //If Not Success,This Step Fail.
                    return false;

                launchUrl = userResult.launchUrl;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion


        public class DetailEbet
        {
            public string tableCode { get; set; }
            public int tableType { get; set; }
            public string roundCode { get; set; }
            public IList<BetListEbet> betList { get; set; }
        }

        public class BetListEbet
        {
            public long betType { get; set; }
        }
    }
}
