using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class N2LiveController : ControllerBase
    {
        private class TokenInfo
        {
            public string shortTimeToken { get; set; }
            public string longTimeToken { get; set; }
            public string siteId { get; set; }
            public string tpCurrency { get; set; }
        }

        #region Request

        public class CommRequest
        {
            public CommReq request { get; set; }
        }

        public class CommReq
        {
            public string userid { get; set; }
            public int vendorid { get; set; }
            public string timestamp { get; set; }
            public string sessiontoken { get; set; }

            [JsonProperty("@action")]
            public string action { get; set; }

            [JsonProperty("@id")]
            public string id { get; set; }
        }

        public class LoginRequest
        {
            public LoginReq request { get; set; }
        }

        public class LoginReq : CommReq
        {
            public string clientip { get; set; }
        }

        public class LoginManualRequest
        {
            public LoginManualReq request { get; set; }
        }

        public class LoginManualReq
        {
            public string userid { get; set; }
            public string password { get; set; }
            public int vendorid { get; set; }
            public string timestamp { get; set; }

            [JsonProperty("@action")]
            public string action { get; set; }

            [JsonProperty("@id")]
            public string id { get; set; }
        }

        public class BalanceRequest
        {
            public BalanceReq request { get; set; }
        }

        public class BalanceReq : CommReq
        {
            public string currencyid { get; set; }
        }

        #endregion

        #region Response

        [XmlRoot(ElementName = "n2root")]
        public class LoginResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public LoginRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public LoginResponse()
            {
                result = new LoginRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class LoginRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "username")]
            public string username { get; set; }

            [XmlElement(ElementName = "acode")]
            public string acode { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "sessiontoken")]
            public string sessiontoken { get; set; }

            [XmlElement(ElementName = "clientip")]
            public string clientip { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class ErrorResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "exception")]
            public ErrorExc exception { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public ErrorResponse()
            {
                exception = new ErrorExc();
            }
        }

        [XmlRoot(ElementName = "exception")]
        public class ErrorExc
        {
            [XmlElement(ElementName = "desc")]
            public string desc { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class BalanceResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public BalanceRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public BalanceResponse()
            {
                result = new BalanceRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class BalanceRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "timestamp")]
            public string timestamp { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class BetResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public BetRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public BetResponse()
            {
                result = new BetRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class BetRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "tradeid")]
            public string tradeid { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "timestamp")]
            public string timestamp { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class BonusResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public BonusRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public BonusResponse()
            {
                result = new BonusRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class BonusRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "bonusid")]
            public string bonusid { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "timestamp")]
            public string timestamp { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class EventResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public EventRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public EventResponse()
            {
                result = new EventRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class EventRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "eventtrxid")]
            public string eventtrxid { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "timestamp")]
            public string timestamp { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        [XmlRoot(ElementName = "n2root")]
        public class SmartBetResponse
        {
            [XmlElement(ElementName = "status")]
            public string status { get; set; }

            [XmlElement(ElementName = "result")]
            public SmartBetRes result { get; set; }

            [XmlAttribute(AttributeName = "n2xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string n2xsd { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string source { get; set; }

            public SmartBetResponse()
            {
                result = new SmartBetRes();
            }
        }

        [XmlRoot(ElementName = "result")]
        public class SmartBetRes
        {
            [XmlElement(ElementName = "userid")]
            public string userid { get; set; }

            [XmlElement(ElementName = "currencyid")]
            public string currencyid { get; set; }

            [XmlElement(ElementName = "balance")]
            public decimal balance { get; set; }

            [XmlElement(ElementName = "schemeid")]
            public string schemeid { get; set; }

            [XmlElement(ElementName = "vendorid")]
            public string vendorid { get; set; }

            [XmlElement(ElementName = "merchantpasscode")]
            public string merchantpasscode { get; set; }

            [XmlElement(ElementName = "timestamp")]
            public string timestamp { get; set; }

            [XmlAttribute(AttributeName = "action")]
            public string action { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public string id { get; set; }
        }

        #endregion

        private readonly ILogger<N2LiveController> _logger;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly string _tpName;
        private readonly IHostEnvironment _environment;
        private readonly int _gameType = 0;

        public N2LiveController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _thisTp = Tp.N2Live;
            _tpName = Enum.GetName(typeof(Tp), _thisTp);
            _gameType = (int)GameType.N2Live_LiveCasino;
        }

        [Route("[controller]")]
        [HttpGet]
        [HttpPost]
        [Produces("application/xml")]
        public async Task<dynamic> SeamlessWalletAsync()
        {
            string logName = $"{_thisTp}";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            ErrorResponse commResp = new ErrorResponse();
            commResp.status = "101";
            commResp.n2xsd = "urn:n2ns";
            commResp.source = "";
            commResp.exception.action = "";
            commResp.exception.id = "";
            commResp.exception.desc = "ERROR_SYSTEM_EXCEPTION";

            var xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);

            try
            {
                // xml to json
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(Content);
                var eleTag = xmlDoc.GetElementsByTagName("trade");
                if (eleTag.Count == 1)
                {
                    var attribute = xmlDoc.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
                    attribute.InnerText = "true";
                    var node = eleTag.Item(0) as XmlElement;
                    node.Attributes.Append(attribute);
                }
                string json = JsonConvert.SerializeXmlNode(xmlDoc.DocumentElement.FirstChild, Newtonsoft.Json.Formatting.Indented, false);

                _log2File.Info(Content, logName);

                var commReq = JsonConvert.DeserializeObject<CommRequest>(json);
                string action = commReq.request.action;
                string id = commReq.request.id;
                logName = $"{logName}_{action}";

                commResp.exception.action = action;
                commResp.exception.id = id;

                bool error = false;

                if (string.Equals(action, "slogin", StringComparison.OrdinalIgnoreCase))
                {
                    // slogin for n2 test account but need use tpservice create account first and add test hierarchy cust id
                    var getLoginManualReq = JsonConvert.DeserializeObject<LoginManualRequest>(json);

                    string getTpCurrencyId = GetTestHierarchy(getLoginManualReq.request.userid, "0", getLoginManualReq.request.password);

                    if (getTpCurrencyId != "1111")
                    {
                        commResp.status = "105";
                        commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }


                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getLoginManualReq.request.userid);
                    if (tpCheck == null)
                    {
                        string token = null;
                        string clientIp = "";
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfo((int)_thisTp, getLoginManualReq.request.userid);
                        _tpCustInfoBLL.AddLoginRecord(tpCustInfo.CustId, tpCustInfo.SiteId, clientIp, out token);
                        _tpCustInfoBLL.AddUserLoginLog(tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, clientIp, $"Web - {_tpName}", $"Web - {_tpName}");
                        tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)_thisTp, getLoginManualReq.request.userid);
                    }
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        commResp.status = "105";
                        commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }

                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getLoginManualReq.request.userid, _gameType);
                    if (tpCustBalance != null)
                    {
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getLoginManualReq.request.userid);
                        var key = _authBLL.ApplyToken(tpCustInfo);
                        var sessionKeyArr = new string[] { key, tpCheck.Token, tpCustInfo.SiteId.ToString(), tpCustBalance.TpCurrencyId };
                        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sessionKeyArr));
                        string sessionKey = Convert.ToBase64String(bytes);

                        var thirdPartySetting = _thirdPartyBLL.GetThirdPartyBySite((int)_thisTp, tpCustInfo.SiteId);

                        LoginResponse getApiResp = new LoginResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getLoginManualReq.request.userid;
                        getApiResp.result.username = getLoginManualReq.request.userid;
                        getApiResp.result.acode = "";
                        getApiResp.result.currencyid = getTpCurrencyId;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                        getApiResp.result.sessiontoken = sessionKey;

                        xml = N2LiveController.ToXml(typeof(LoginResponse), getApiResp);
                        _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }

                    xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                    _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                    return xml;
                }
                else
                {
                    var tokenInfo = GetTokenInfo(commReq.request.sessiontoken);
                    var thirdPartySetting = _thirdPartyBLL.GetThirdPartyBySite((int)_thisTp, Convert.ToInt32(tokenInfo.siteId));

                    if (string.Equals(action, "swebsinglelogin", StringComparison.OrdinalIgnoreCase))
                    {
                        var getLoginReq = JsonConvert.DeserializeObject<LoginRequest>(json);

                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getLoginReq.request.userid);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            error = true;
                            commResp.status = "105";
                            commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                        }
                        else if (tpCheck.Token != tokenInfo.longTimeToken)
                        {
                            error = true;
                            commResp.status = "118";
                            commResp.exception.desc = "ERROR_INVALID_SESSION_TOKEN";
                        }

                        if (error == false)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getLoginReq.request.userid, _gameType);
                            if (tpCustBalance != null)
                            {
                                LoginResponse getApiResp = new LoginResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getLoginReq.request.userid;
                                getApiResp.result.username = getLoginReq.request.userid;
                                getApiResp.result.acode = "";
                                getApiResp.result.currencyid = GetTestHierarchy(getLoginReq.request.userid, tpCustBalance.TpCurrencyId);
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.sessiontoken = getLoginReq.request.sessiontoken;
                                getApiResp.result.clientip = tpCheck.IP;

                                xml = N2LiveController.ToXml(typeof(LoginResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "sgetbalance", StringComparison.OrdinalIgnoreCase))
                    {
                        var getBalanceReq = JsonConvert.DeserializeObject<BalanceRequest>(json);

                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getBalanceReq.request.userid);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            error = true;
                            commResp.status = "105";
                            commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";

                        }
                        else if (tpCheck.Token != tokenInfo.longTimeToken)
                        {
                            error = true;
                            commResp.status = "118";
                            commResp.exception.desc = "ERROR_INVALID_SESSION_TOKEN";
                        }

                        if (error == false)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getBalanceReq.request.userid, _gameType);
                            if (tpCustBalance != null)
                            {
                                BalanceResponse getApiResp = new BalanceResponse();
                                getApiResp.result = new BalanceRes();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getBalanceReq.request.userid;
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.currencyid = GetTestHierarchy(getBalanceReq.request.userid, tpCustBalance.TpCurrencyId);
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(BalanceResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "splacetrade", StringComparison.OrdinalIgnoreCase))
                    {
                        var getBetReq = JsonConvert.DeserializeObject<BettingDataLC_N2Live>(json);

                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getBetReq.request.userid);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            error = true;
                            commResp.status = "105";
                            commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                        }
                        else if (tpCheck.Token != tokenInfo.longTimeToken)
                        {
                            error = true;
                            commResp.status = "118";
                            commResp.exception.desc = "ERROR_INVALID_SESSION_TOKEN";
                        }

                        if (error == false)
                        {
                            try
                            {
                                var result = _bettingBLL.PlaceBetLC_N2Live(getBetReq, out decimal tpAffterAmount);
                                if (result > 0)
                                {
                                    BetResponse getApiResp = new BetResponse();
                                    getApiResp.status = "0";
                                    getApiResp.n2xsd = "urn:n2ns";
                                    getApiResp.source = "";
                                    getApiResp.result.action = action;
                                    getApiResp.result.id = id;
                                    getApiResp.result.userid = getBetReq.request.userid;
                                    getApiResp.result.currencyid = GetTestHierarchy(getBetReq.request.userid, tokenInfo.tpCurrency);
                                    getApiResp.result.balance = tpAffterAmount;
                                    getApiResp.result.tradeid = getBetReq.request.tradeid;
                                    getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                    getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                    getApiResp.result.timestamp = GetCurrentGameTime();

                                    xml = N2LiveController.ToXml(typeof(BetResponse), getApiResp);
                                    _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                    return xml;
                                }
                                else if (result == -1)
                                {
                                    commResp.status = "116";
                                    commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                                }
                                else if (tpAffterAmount < 0)
                                {
                                    commResp.status = "113";
                                    commResp.exception.desc = "ERROR_INVALID_AMOUNT";
                                }
                            }
                            catch (Exception ex)
                            {
                                _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                                return xml;
                            }
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "spushtrade", StringComparison.OrdinalIgnoreCase))
                    {
                        var getCancelBetReq = JsonConvert.DeserializeObject<CancelBetDataLC_N2Live>(json);
                        try
                        {
                            var result = _settledBLL.CancelBetLC_N2Live(getCancelBetReq, out decimal tpAffterAmount);
                            if (result > 0 || result == -2)
                            {
                                // your side should response success to the push trade since the trade placement is failed
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getCancelBetReq.request.userid, _gameType);
                                BetResponse getApiResp = new BetResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getCancelBetReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getCancelBetReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.tradeid = getCancelBetReq.request.tradeid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(BetResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "straderesult", StringComparison.OrdinalIgnoreCase))
                    {
                        var getPayoutReq = JsonConvert.DeserializeObject<PayoutDataLC_N2Live>(json);

                        try
                        {
                            var result = _settledBLL.PayoutLC_N2Live(getPayoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getPayoutReq.request.userid, _gameType);
                                BetResponse getApiResp = new BetResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getPayoutReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getPayoutReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.tradeid = getPayoutReq.request.tradeid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(BetResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "ssmartbetcreate", StringComparison.OrdinalIgnoreCase))
                    {
                        var getSmartBetReq = JsonConvert.DeserializeObject<SmartBettingDataLC_N2Live>(json);

                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getSmartBetReq.request.userid);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            error = true;
                            commResp.status = "105";
                            commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                        }
                        else if (tpCheck.Token != tokenInfo.longTimeToken)
                        {
                            error = true;
                            commResp.status = "118";
                            commResp.exception.desc = "ERROR_INVALID_SESSION_TOKEN";
                        }

                        if (error == false)
                        {
                            try
                            {
                                var result = _bettingBLL.SmartPlaceBetLC_N2Live(getSmartBetReq, out decimal tpAffterAmount);
                                if (result > 0)
                                {
                                    SmartBetResponse getApiResp = new SmartBetResponse();
                                    getApiResp.status = "0";
                                    getApiResp.n2xsd = "urn:n2ns";
                                    getApiResp.source = "";
                                    getApiResp.result.action = action;
                                    getApiResp.result.id = id;
                                    getApiResp.result.userid = getSmartBetReq.request.userid;
                                    getApiResp.result.currencyid = GetTestHierarchy(getSmartBetReq.request.userid, tokenInfo.tpCurrency);
                                    getApiResp.result.balance = tpAffterAmount;
                                    getApiResp.result.schemeid = getSmartBetReq.request.schemeid;
                                    getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                    getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                    getApiResp.result.timestamp = GetCurrentGameTime();

                                    xml = N2LiveController.ToXml(typeof(SmartBetResponse), getApiResp);
                                    _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                    return xml;
                                }
                                else if (result == -1)
                                {
                                    commResp.status = "116";
                                    commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                                }
                                else if (tpAffterAmount < 0)
                                {
                                    commResp.status = "113";
                                    commResp.exception.desc = "ERROR_INVALID_AMOUNT";
                                }
                            }
                            catch (Exception ex)
                            {
                                _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                                return xml;
                            }
                        }
                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "spushsmartbet", StringComparison.OrdinalIgnoreCase))
                    {
                        var getSmartCancelBetReq = JsonConvert.DeserializeObject<SmartCancelBetDataLC_N2Live>(json);
                        try
                        {
                            var result = _settledBLL.SmartCancelBetLC_N2Live(getSmartCancelBetReq, out decimal tpAffterAmount);
                            if (result > 0 || result == -2)
                            {
                                // your side should response success to the push trade since the trade placement is failed
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getSmartCancelBetReq.request.userid, _gameType);
                                SmartBetResponse getApiResp = new SmartBetResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getSmartCancelBetReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getSmartCancelBetReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.schemeid = getSmartCancelBetReq.request.schemeid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(SmartBetResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "ssmartbetclose", StringComparison.OrdinalIgnoreCase))
                    {
                        var getSmartPayoutReq = JsonConvert.DeserializeObject<SmartPayoutDataLC_N2Live>(json);

                        try
                        {
                            var result = _settledBLL.SmartPayoutLC_N2Live(getSmartPayoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getSmartPayoutReq.request.userid, _gameType);
                                SmartBetResponse getApiResp = new SmartBetResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getSmartPayoutReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getSmartPayoutReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpCustBalance.Balance;
                                getApiResp.result.schemeid = getSmartPayoutReq.request.schemeid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(SmartBetResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "screditbonus", StringComparison.OrdinalIgnoreCase))
                    {
                        var getBonusReq = JsonConvert.DeserializeObject<BonusDataLC_N2Live>(json);

                        try
                        {
                            var result = _settledBLL.BonusLC_N2Live(getBonusReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                BonusResponse getApiResp = new BonusResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getBonusReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getBonusReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpAffterAmount;
                                getApiResp.result.bonusid = getBonusReq.request.bonusid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(BonusResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                    else if (string.Equals(action, "seventtransaction", StringComparison.OrdinalIgnoreCase))
                    {
                        var getEventReq = JsonConvert.DeserializeObject<EventDataLC_N2Live>(json);

                        try
                        {
                            var result = _settledBLL.EventLC_N2Live(getEventReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                EventResponse getApiResp = new EventResponse();
                                getApiResp.status = "0";
                                getApiResp.n2xsd = "urn:n2ns";
                                getApiResp.source = "";
                                getApiResp.result.action = action;
                                getApiResp.result.id = id;
                                getApiResp.result.userid = getEventReq.request.userid;
                                getApiResp.result.currencyid = GetTestHierarchy(getEventReq.request.userid, tokenInfo.tpCurrency);
                                getApiResp.result.balance = tpAffterAmount;
                                getApiResp.result.eventtrxid = getEventReq.request.eventtrxid;
                                getApiResp.result.vendorid = thirdPartySetting.TpSetting["VendorId"];
                                getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting["MerchantPassCode"];
                                getApiResp.result.timestamp = GetCurrentGameTime();

                                xml = N2LiveController.ToXml(typeof(EventResponse), getApiResp);
                                _log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                                return xml;
                            }
                            else if (result == -1)
                            {
                                commResp.status = "116";
                                commResp.exception.desc = "ERROR_DUPLICATE_TRANSACTION";
                            }
                            else if (tpAffterAmount < 0)
                            {
                                commResp.status = "113";
                                commResp.exception.desc = "ERROR_INVALID_AMOUNT";
                            }
                        }
                        catch (Exception ex)
                        {
                            _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                            return xml;
                        }

                        xml = N2LiveController.ToXml(typeof(ErrorResponse), commResp);
                        _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                }
                _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            catch (Exception ex)
            {
                _log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName, ex);
                return xml;
            }
        }

        private static string GetCurrentGameTime()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");
        }

        private static TokenInfo GetTokenInfo(string token)
        {
            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return new TokenInfo
            {
                shortTimeToken = sessionKeyArr[0], /* 1 min*/
                longTimeToken = sessionKeyArr[1],  /* Long Time*/
                siteId = sessionKeyArr[2],
                tpCurrency = sessionKeyArr[3]
            };
        }

        private static dynamic ToXml(dynamic type, object response)
        {
            var xml = "";
            XmlSerializerNamespaces ns = new();
            ns.Add("", "");

            XmlSerializer serilizer = new(type);
            using (var sww = new StringWriter())
            {
                using XmlWriter writer = XmlWriter.Create(sww);
                serilizer.Serialize(writer, response, ns);
                xml = sww.ToString();
            }

            var xml2 = xml.Replace("n2root", "n2xsd:n2root");
            XmlDocument doc = new();
            doc.LoadXml(xml2);

            return doc;
        }

        private string GetTestHierarchy(string account, string tpCurrencyId, string password = null)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, account);
            List<long> testHierarchyCustId = _environment.IsDevelopment() ? new List<long> { 1288, 1481 } : new List<long> { 90, 31909, 240129, 236358, 2071, 2072 };
            bool isTestHierarchy = testHierarchyCustId.Contains(tpCustInfo.CustId);
            if (isTestHierarchy)
            {
                tpCurrencyId = "1111";
            }

            if (password != null && tpCustInfo.TpPwd != password)
            {
                tpCurrencyId = "0";
            }

            return tpCurrencyId;
        }
    }
}
