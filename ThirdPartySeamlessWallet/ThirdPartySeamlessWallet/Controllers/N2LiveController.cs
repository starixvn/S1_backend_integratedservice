using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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

        private readonly ILogger<N2LiveController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;

        public N2LiveController(
            ILogger<N2LiveController> logger,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            Log2File log2File,
            Tools tools
        )
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.tools = tools;
            thisTp = Tp.N2Live;
        }

        [Route("[controller]")]
        [HttpGet]
        [HttpPost]
        [Produces("application/xml")]
        public async Task<dynamic> SeamlessWalletAsync()
        {
            string logName = $"{thisTp}";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

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

            log2File.Info(Content, logName);

            var commReq = JsonConvert.DeserializeObject<CommRequest>(json);
            string action = commReq.request.action;
            string id = commReq.request.id;
            logName = $"{logName}_{action}";

            ErrorResponse commResp = new ErrorResponse();
            commResp.status = "101";
            commResp.n2xsd = "urn:n2ns";
            commResp.source = "";
            commResp.exception.action = action;
            commResp.exception.id = id;
            commResp.exception.desc = "ERROR_SYSTEM_EXCEPTION";

            var xml = toXml(typeof(ErrorResponse), commResp);

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            var tokenInfo = getTokenInfo(commReq.request.sessiontoken);

            bool error = false;

            if (string.Equals(action, "swebsinglelogin", StringComparison.OrdinalIgnoreCase))
            {
                var getLoginReq = JsonConvert.DeserializeObject<LoginRequest>(json);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getLoginReq.request.userid);
                if (tpCheck == null)
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
                else if (tpCheck.Locked)
                {
                    error = true;
                    commResp.status = "105";
                    commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                }

                if (error == false)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getLoginReq.request.userid);
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
                        getApiResp.result.currencyid = tpCustBalance.TpCurrencyId;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.sessiontoken = getLoginReq.request.sessiontoken;
                        getApiResp.result.clientip = tpCheck.IP;

                        xml = toXml(typeof(LoginResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "sgetbalance", StringComparison.OrdinalIgnoreCase))
            {
                var getBalanceReq = JsonConvert.DeserializeObject<BalanceRequest>(json);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.request.userid);
                if (tpCheck == null)
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
                else if (tpCheck.Locked)
                {
                    error = true;
                    commResp.status = "105";
                    commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                }

                if (error == false)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.request.userid);
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
                        getApiResp.result.currencyid = tpCustBalance.TpCurrencyId;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(BalanceResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                        return xml;
                    }
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "splacetrade", StringComparison.OrdinalIgnoreCase))
            {
                var getBetReq = JsonConvert.DeserializeObject<BettingDataLC_N2Live>(json);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBetReq.request.userid);
                if (tpCheck == null)
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
                else if (tpCheck.Locked)
                {
                    error = true;
                    commResp.status = "105";
                    commResp.exception.desc = "ERROR_INVALID_AUTHENTICATION";
                }

                if (error == false)
                {
                    try
                    {
                        var result = bettingBLL.PlaceBetLC_N2Live(getBetReq, out decimal tpAffterAmount);
                        if (result > 0)
                        {
                            BetResponse getApiResp = new BetResponse();
                            getApiResp.status = "0";
                            getApiResp.n2xsd = "urn:n2ns";
                            getApiResp.source = "";
                            getApiResp.result.action = action;
                            getApiResp.result.id = id;
                            getApiResp.result.userid = getBetReq.request.userid;
                            getApiResp.result.currencyid = tokenInfo.tpCurrency;
                            getApiResp.result.balance = tpAffterAmount;
                            getApiResp.result.tradeid = getBetReq.request.tradeid;
                            getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                            getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                            getApiResp.result.timestamp = getCurrentGameTime();

                            xml = toXml(typeof(BetResponse), getApiResp);
                            log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                        log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                        return xml;
                    }
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "spushtrade", StringComparison.OrdinalIgnoreCase))
            {
                var getCancelBetReq = JsonConvert.DeserializeObject<CancelBetDataLC_N2Live>(json);
                try
                {
                    var result = settledBLL.CancelBetLC_N2Live(getCancelBetReq, out decimal tpAffterAmount);
                    if (result > 0 || result == -2)
                    {
                        // your side should response success to the push trade since the trade placement is failed
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getCancelBetReq.request.userid);
                        BetResponse getApiResp = new BetResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getCancelBetReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.tradeid = getCancelBetReq.request.tradeid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(BetResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "straderesult", StringComparison.OrdinalIgnoreCase))
            {
                var getPayoutReq = JsonConvert.DeserializeObject<PayoutDataLC_N2Live>(json);

                try
                {
                    var result = settledBLL.PayoutLC_N2Live(getPayoutReq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getPayoutReq.request.userid);
                        BetResponse getApiResp = new BetResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getPayoutReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.tradeid = getPayoutReq.request.tradeid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(BetResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "ssmartbetcreate", StringComparison.OrdinalIgnoreCase))
            {
                var getSmartBetReq = JsonConvert.DeserializeObject<SmartBettingDataLC_N2Live>(json);

                try
                {
                    var result = bettingBLL.SmartPlaceBetLC_N2Live(getSmartBetReq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        SmartBetResponse getApiResp = new SmartBetResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getSmartBetReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpAffterAmount;
                        getApiResp.result.schemeid = getSmartBetReq.request.schemeid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(SmartBetResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "spushsmartbet", StringComparison.OrdinalIgnoreCase))
            {
                var getSmartCancelBetReq = JsonConvert.DeserializeObject<SmartCancelBetDataLC_N2Live>(json);
                try
                {
                    var result = settledBLL.SmartCancelBetLC_N2Live(getSmartCancelBetReq, out decimal tpAffterAmount);
                    if (result > 0 || result == -2)
                    {
                        // your side should response success to the push trade since the trade placement is failed
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getSmartCancelBetReq.request.userid);
                        SmartBetResponse getApiResp = new SmartBetResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getSmartCancelBetReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.schemeid = getSmartCancelBetReq.request.schemeid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(SmartBetResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "ssmartbetclose", StringComparison.OrdinalIgnoreCase))
            {
                var getSmartPayoutReq = JsonConvert.DeserializeObject<SmartPayoutDataLC_N2Live>(json);

                try
                {
                    var result = settledBLL.SmartPayoutLC_N2Live(getSmartPayoutReq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getSmartPayoutReq.request.userid);
                        SmartBetResponse getApiResp = new SmartBetResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getSmartPayoutReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpCustBalance.Balance;
                        getApiResp.result.schemeid = getSmartPayoutReq.request.schemeid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(SmartBetResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "screditbonus", StringComparison.OrdinalIgnoreCase))
            {
                var getBonusReq = JsonConvert.DeserializeObject<BonusDataLC_N2Live>(json);

                try
                {
                    var result = settledBLL.BonusLC_N2Live(getBonusReq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        BonusResponse getApiResp = new BonusResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getBonusReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpAffterAmount;
                        getApiResp.result.bonusid = getBonusReq.request.bonusid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(BonusResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }
            else if (string.Equals(action, "seventtransaction", StringComparison.OrdinalIgnoreCase))
            {
                var getEventReq = JsonConvert.DeserializeObject<EventDataLC_N2Live>(json);

                try
                {
                    var result = settledBLL.EventLC_N2Live(getEventReq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        EventResponse getApiResp = new EventResponse();
                        getApiResp.status = "0";
                        getApiResp.n2xsd = "urn:n2ns";
                        getApiResp.source = "";
                        getApiResp.result.action = action;
                        getApiResp.result.id = id;
                        getApiResp.result.userid = getEventReq.request.userid;
                        getApiResp.result.currencyid = tokenInfo.tpCurrency;
                        getApiResp.result.balance = tpAffterAmount;
                        getApiResp.result.eventtrxid = getEventReq.request.eventtrxid;
                        getApiResp.result.vendorid = thirdPartySetting.TpSetting[$"VendorId_{tokenInfo.siteId}"];
                        getApiResp.result.merchantpasscode = thirdPartySetting.TpSetting[$"MerchantPassCode_{tokenInfo.siteId}"];
                        getApiResp.result.timestamp = getCurrentGameTime();

                        xml = toXml(typeof(EventResponse), getApiResp);
                        log2File.Info($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
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
                    log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}\r\nException:\r\n{ex}", logName);
                    return xml;
                }

                xml = toXml(typeof(ErrorResponse), commResp);
                log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
                return xml;
            }

            log2File.Error($"Request:\r\n{Content}\r\nResponse:\r\n{xml.OuterXml}", logName);
            return xml;
        }

        private string getCurrentGameTime()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff");
        }

        private TokenInfo getTokenInfo(string token)
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

        private dynamic toXml(dynamic type, object response)
        {
            var xml = "";
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            XmlSerializer serilizer = new XmlSerializer(type);
            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    serilizer.Serialize(writer, response, ns);
                    xml = sww.ToString();
                }
            }

            var xml2 = xml.Replace("n2root", "n2xsd:n2root");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml2);

            return doc;
        }
        /*
                private dynamic toXmlV3(string action, string id, string stat, object response)
                {
                    XmlDocument doc = new XmlDocument();
                    //doc.PrependChild(doc.CreateXmlDeclaration("1.0", "utf-16", null));

                    // root
                    XmlElement root = doc.CreateElement("n2xsd:n2root", "n2root");
                    root.SetAttribute("xmlns:n2xsd", "urn:n2ns");
                    root.SetAttribute("source", "");

                    // status
                    XmlElement status = doc.CreateElement("status");
                    status.InnerText = stat;

                    // append
                    doc.AppendChild(root);
                    root.AppendChild(status);

                    if (stat == "0")
                    {
                        // result
                        string jsonText = JsonConvert.SerializeObject(response);
                        XmlDocument resultXml = JsonConvert.DeserializeXmlNode(jsonText, "result", true);

                        XmlElement result = doc.CreateElement("result");
                        result.SetAttribute("action", action);
                        result.SetAttribute("id", id);
                        result.InnerXml = resultXml.FirstChild.FirstChild.InnerXml;
                        root.AppendChild(result);
                    }
                    else
                    {
                        // exception
                        XmlElement exception = doc.CreateElement("exception");
                        exception.SetAttribute("action", action);
                        exception.SetAttribute("id", id);
                        XmlElement desc = doc.CreateElement("desc");
                        desc.InnerText = stat;
                        exception.AppendChild(desc);
                        root.AppendChild(exception);
                    }

                    doc.LoadXml(doc.InnerXml);
                    return doc;
                }
        */
    }
}
