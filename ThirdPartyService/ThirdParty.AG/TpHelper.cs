using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;


namespace ThirdParty.AG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, AGConfig> _siteAGConfig;
        private readonly ILogger<TpHelper> _logger;
        public TpHelper(IServiceProvider provider)
        {
            _siteAGConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AGConfig>(TpEnum.AG.ToString());
            _logger = provider.GetRequiredService<ILogger<TpHelper>>();
        }
        
        Dictionary<string, string> oddsTypeDic = new Dictionary<string, string>() {
            {"MYR","GF4B" },
            {"CNY","C" },
            {"THB","GF4A" },
            {"IDR","GF4A" },
            {"VND","GF4A" },
            {"USD","GF4A" },
            {"INR","GF4A" },
            {"JPY","A" },
            {"KRW","A" },
            {"BRL","A" }
        };

        readonly Dictionary<string, int> langIdDic = new Dictionary<string, int>()
        {
            {"zh-tw", 2},
            {"zh-cn", 1},
            {"en-us", 3},
            {"id", 11},
            {"th", 6},
            {"vi", 8},
            {"khm", 9},
            {"jp", 4},
            {"kr", 5},
            {"br", 23},
        };
        public class result
        {
            public string info { get; set; }
            public string msg { get; set; }
        }

        public class AGConfig : TpSiteConfig
        {
            public string DesKey { get; set; }
            public string Md5Key { get; set; }
            public string cagent { get; set; }
            public string HashKey { get; set; }
            public string CommonUrl { get; set; }
            public string SessionUrl { get; set; }
            public string data_cagent { get; set; }
        }

        #region Response

        public class BalanceResponse
        {
            public string ResponseCode { get; set; }
            public decimal Balance { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var actype = tpCustInfo.IsTest ? "0" : "1";
            var config = _siteAGConfig[tpCustInfo.SiteId];
            var cagent = config.cagent; 
            var apiUrl = config.ApiUrl; 
            var Md5key = config.Md5Key; 
                
            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/method=lg/\\\\/actype={actype}/\\\\/password={tpCustInfo.TpPwd}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}";
            _logger.LogInformation(sParams);
            string eParams = DESEncrypt(sParams, config.DesKey);
            string oMD5 = $"{eParams}{Md5key}";
            string postParams = $"params={eParams}&key={ToMD5(oMD5).ToLower()}";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(apiUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();

            if (IsXML(sResponse))
            {
                var oCreateMemberResp = FromXml<result>(sResponse);
                switch (oCreateMemberResp.info.ToString())
                {
                    case "0":
                        oApiResult.ErrorCode = ErrorCode.Success;
                        oApiResult.Message = oCreateMemberResp.msg;
                        break;
                    case "key_error":
                        oApiResult.ErrorCode = ErrorCode.KEY_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "network_error":
                        oApiResult.ErrorCode = ErrorCode.NETWORK_ERROR;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "account_add_fail":
                        oApiResult.ErrorCode = ErrorCode.Fail;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    case "error":
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                    default:
                        oApiResult.ErrorCode = ErrorCode.NoAuthority;
                        oApiResult.RawMessage = oCreateMemberResp.msg;
                        break;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = sResponse;
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
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.ThirdId = $"";
            transIdModel.Implemented = false;

            return transIdModel;

        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var actype = tpCustInfo.IsTest ? "0" : "1";
            var config = _siteAGConfig[tpCustInfo.SiteId];
            var cagent = config.cagent;
            var data_cagent = config.data_cagent;
            var GameUrl = config.GameUrl;
            var Md5key = config.Md5Key;
            var SessionUrl = config.SessionUrl;
            
            string _oddtype = null;
            oddsTypeDic.TryGetValue(tpCustInfo.TpCurrencyId, out _oddtype);
            string oddtype = _oddtype;

            string balance = GetBalance(tpCustInfo.TpUserName);
            string token = "";
            inputs.TryGetValue("Token", out token);
            string postParams = $"productid={data_cagent}&username={tpCustInfo.TpUserName}&session_token={token}&credit={balance}";
            _logger.LogInformation(postParams);
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["User-Agent"] = $"WEB_LIB_GI_{cagent}";

            string sResponse = HttpGet(SessionUrl, postParams, "application/x-www-form-urlencoded", header);
            ApiResult oApiResult = new ApiResult();
            {
                if (IsXML(sResponse))
                {
                    var ResponseCode = FromXmlNode(sResponse);
                    switch (ResponseCode.ToString())
                    {
                        case "OK":
                            break;
                        default:
                            oApiResult.ErrorCode = ErrorCode.ServerResponseError;
                            oApiResult.RawMessage = ResponseCode;
                            return oApiResult;
                    }
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = sResponse;
                    return oApiResult;
                }
            }

            // parameter handle
            long sTimestemp = GetTimestemp1();

            string dm = string.Empty;
            if (inputs.ContainsKey("homeUrl"))
            {
                dm = inputs["homeUrl"];
            }

            var langId = langIdDic.TryGetValue(TpLangId, out int _langId) ? _langId : 3;

            string sParams = $"cagent={cagent}/\\\\/loginname={tpCustInfo.TpUserName}/\\\\/password={tpCustInfo.TpPwd}/\\\\/actype={actype}/\\\\/oddtype={oddtype}/\\\\/cur={tpCustInfo.TpCurrencyId}/\\\\/dm={dm}/\\\\/sid={cagent}{sTimestemp}/\\\\/lang={langId}";

            if (inputs.ContainsKey("gameType") && !string.IsNullOrEmpty(inputs["gameType"]))
            {
                sParams = $"{sParams}/\\\\/gameType={inputs["gameType"]}";
            }

            // encrypt
            string eParams = DESEncrypt(sParams, config.DesKey);
            string oMD5 = $"{eParams}{Md5key}";
            postParams = $"params={eParams}&key={ToMD5(oMD5).ToLower()}";

            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = $"{GameUrl}?{postParams}";
            return oApiResult;
        }


        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var billNoList = TpBetRecordProvider.DbQuery("Service_BettransLC_AGV2_Payout_Get", new { transactionID = getBetDetail.TransId }, DataBase.Report)
                .Select(item => item.GetValueOrDefault("billNo")?.ToString());

            var wagerDetailList = TpBetRecordProvider.DbQuery("Service_TpWager_AG_Detail_Get",
                new { billNoList = JsonConvert.SerializeObject(billNoList) }, DataBase.Wager);

            var detail = ToJson(wagerDetailList);
            var gameCode = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("gameType")?.ToString();
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("gameCode")?.ToString(),
                    TpUserName = wagerDetailList.FirstOrDefault()?.GetValueOrDefault("playName")?.ToString(),
                    UTCBetTime = wagerDetailList.Min(i => (DateTime)i.GetValueOrDefault("betTime", DateTime.MinValue)),
                    UTCSettledTime = wagerDetailList.Max(i => (DateTime)i.GetValueOrDefault("recalcuTime", DateTime.MinValue)),
                    Stake = wagerDetailList.Sum(i => (decimal)i.GetValueOrDefault("betAmount")) * tpCurrency.ExchangeRate,
                    WinlostAmount = wagerDetailList.Sum(i => (decimal)i.GetValueOrDefault("netAmount")) * tpCurrency.ExchangeRate,
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
            var billNoList = TpBetRecordProvider.DbQuery("Service_BettransLC_AGV2_Payout_Get", new { transactionID = betRecordInfo.TransId }, DataBase.Main)
            .Select(item => item.GetValueOrDefault("billNo")?.ToString());
            var detail = TpBetRecordProvider.DbQuery("Service_TpWager_AG_Detail_Get", new { billNoList = JsonConvert.SerializeObject(billNoList) }, DataBase.Wager);
            var gameCode = detail.FirstOrDefault()?.GetValueOrDefault("gameType")?.ToString();

            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = detail.FirstOrDefault()?.GetStringValue("gameCode"),
                    Title = tpGameType.TypeName,
                    MatchID = detail.FirstOrDefault()?.GetStringValue("tableCode"),
                    BetType = detail.FirstOrDefault()?.GetStringValue("playType"),
                    BetChoice = string.Empty,
                    Result = detail.FirstOrDefault()?.GetStringValue("result"),
                    DeviceType = GetDeviceType(detail.FirstOrDefault()?.GetStringValue("deviceType")),
                    IP = detail.FirstOrDefault()?.GetStringValue("betIP"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }
        private DeviceType GetDeviceType(string device)
        {
            switch (device)
            {
                case "0":
                case "7":
                    return DeviceType.Desktop;
                case "1":
                case "3":
                case "6":
                    return DeviceType.Mobile;
                default:
                    return DeviceType.None;
            }
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "BAC" => "Baccarat",
                "CBAC" => "VIP Baccarat",
                "DT" => "Dragon Tiger",
                "SHB" => "Sicbo",
                "ROU" => "Roulette",
                "LBAC" => "Bid Baccarat",
                "NN" => "Bull Bull",
                "BJ" => "Blackjack",
                "ZJH" => "Win Three Cards",
                "BF" => "Bull Fight",
                "SG" => "Three Face",
                _ => gameCode,
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        protected T FromXml<T>(string xml) where T : new()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlElement root = doc.DocumentElement;

            XmlAttributeCollection nodeList = root.Attributes;

            var data = new T();

            foreach (var key in data.GetType().GetProperties())
            {
                key.SetValue(data, nodeList.GetNamedItem(key.Name).Value);

            }
            return data;
        }

        protected string FromXmlNode(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlElement root = doc.DocumentElement;
            XmlAttributeCollection nodeList = root.Attributes;
            XmlNodeList nodes = doc.DocumentElement.SelectNodes("/PlayerTicketResponse");
            return nodes[0].SelectSingleNode("ResponseCode").InnerText; ;
        }

        private string GetBalance(string TpUserName)
        {
            var postData = new
            {
                TpUserName = TpUserName
            };

            var parameters = JsonConvert.SerializeObject(postData);
            var result = HttpPost(
                $"{SeamlessServiceHost}AG/getBalance",
                parameters,
                "application/json"
            );

            if (!IsJSON(result))
            {
                return "0";
            }

            var response = JsonConvert.DeserializeObject<BalanceResponse>(result);
            return response.Balance.ToString();
        }
    }
}
