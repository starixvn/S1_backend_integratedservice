using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class GPIController : ControllerBase
    {
        private readonly Dictionary<string, string> lCountryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"RMB","CN" },
            {"IDR","ID" },
            {"MYR","MY" },
            {"THB","TH" },
            {"VND","VN" },
            {"USD","KH" },
            {"INR","INR"}
        };

        private readonly AuthBLL authBLL;
        private readonly ILogger<GPIController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly int gameType = 0;

        public GPIController(ILogger<GPIController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            gameType = (int)GameType.GPI_RNG;
        }

        [XmlRoot(ElementName = "resp")]
        public class ValidateTicketResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "cust_id")]
            public string Cust_id { get; set; }
            [XmlElement(ElementName = "cust_name")]
            public string Cust_name { get; set; }
            [XmlElement(ElementName = "currency_code")]
            public string Currency_code { get; set; }
            [XmlElement(ElementName = "language")]
            public string Language { get; set; }
            [XmlElement(ElementName = "test_cust")]
            public bool Test_cust { get; set; }
            [XmlElement(ElementName = "country")]
            public string Country { get; set; }
            [XmlElement(ElementName = "date_of_birth")]
            public string Date_of_birth { get; set; }
            [XmlElement(ElementName = "ip")]
            public string Ip { get; set; }
        }

        [Route("[controller]/ms/validateticket")]
        [HttpPost]
        [Produces("application/xml")]
        public ValidateTicketResp validateticket([FromForm] string ticket)
        {
            logger.LogCritical(ticket);

            ValidateTicketResp validateTicketResp = new ValidateTicketResp();
            TpCustInfoAuth tpCustInfoAuth = null;
            if (authBLL.Authenticate(ticket, out tpCustInfoAuth))
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.GPI, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    validateTicketResp.Error_code = -3;
                    validateTicketResp.Error_msg = "Authentication Failed";
                }
                else if (tpCheck.Locked)
                {
                    validateTicketResp.Error_code = -3;
                    validateTicketResp.Error_msg = "Authentication Failed";
                }
                else
                {
                    validateTicketResp.Error_code = 0;
                    validateTicketResp.Cust_id = tpCustInfoAuth.TpUserName;
                    validateTicketResp.Cust_name = tpCustInfoAuth.TpUserName;
                    validateTicketResp.Currency_code = tpCustInfoAuth.TpCurrencyId;
                    validateTicketResp.Country = lCountryMapping[tpCustInfoAuth.TpCurrencyId];
                    validateTicketResp.Language = tpCustInfoAuth.TpLangId;
                    validateTicketResp.Test_cust = tpCustInfoAuth.IsTest;
                    validateTicketResp.Date_of_birth = "30-7-1970";
                    validateTicketResp.Ip = tpCustInfoAuth.ClientIP;
                }
            }
            else
            {
                validateTicketResp.Error_code = -3;
                validateTicketResp.Error_msg = "Authentication Failed";
            }
            logger.LogCritical(Newtonsoft.Json.JsonConvert.SerializeObject(validateTicketResp));
            return validateTicketResp;
        }

        public class GetbalanceReq
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
            public string cust_id { get; set; }
            public string currency { get; set; }
            public string Operator { get; set; }
            public string game_id { get; set; }
            public string platform { get; set; }
        }


        [XmlRoot(ElementName = "resp")]
        public class GetbalanceResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "balance")]
            public decimal Balance { get; set; }
        }

        [Route("[controller]/ms/getbalance")]
        [HttpPost]
        [Produces("application/xml")]
        public GetbalanceResp getbalance([FromForm] GetbalanceReq getbalanceReq)
        {
            logger.LogCritical(JsonConvert.SerializeObject(getbalanceReq));

            GetbalanceResp getbalanceResp = new GetbalanceResp();

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.GPI, getbalanceReq.cust_id, gameType);
            if (getbalanceReq.currency != tpCustBalance.TpCurrencyId)
            {
                getbalanceResp.Error_code = -29;
                getbalanceResp.Error_msg = "Currency mismatch";
            }
            else
            {
                getbalanceResp.Balance = tpCustBalance.Balance;
            }

            return getbalanceResp;
        }
        public class DebitReq : BettingDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class DebitResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "before")]
            public decimal Before { get; set; }
            [XmlElement(ElementName = "after")]
            public decimal After { get; set; }
            [XmlElement(ElementName = "exchange_rate")]
            public decimal Exchange_rate { get; set; }
            [XmlElement(ElementName = "trx_id")]
            public string Trx_id { get; set; }
        }
        [Route("[controller]/ms/debit")]
        [HttpPost]
        [Produces("application/xml")]
        public DebitResp debit([FromForm] DebitReq debitReq)
        {
            logger.LogCritical(JsonConvert.SerializeObject(debitReq));

            DebitResp debitResp = new DebitResp();
            debitResp.Error_code = -1;
            debitResp.Error_msg = "Unknown error";

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.GPI, debitReq.cust_id);
            if (tpCheck == null)
            {
                debitResp.Error_code = -7;
                debitResp.Error_msg = "Customer is blocked";
            }
            else if (tpCheck.Locked)
            {
                debitResp.Error_code = -7;
                debitResp.Error_msg = "Customer is blocked";
            }
            else
            {
                try
                {
                    if (bettingBLL.PlaceBetRNG_GPI(debitReq, out decimal tpAffterAmount, out string TransId) > 0)
                    {
                        debitResp.Error_code = 0;
                        debitResp.Error_msg = "No errors";
                        if (debitReq.amount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.GPI, debitReq.cust_id, gameType);
                            debitResp.After = tpCustBalance.Balance;
                            debitResp.Before = debitResp.After;
                        }
                        else
                        {
                            debitResp.After = tpAffterAmount;
                            debitResp.Before = tpAffterAmount - debitReq.amount;
                        }
                        debitResp.Exchange_rate = 1;
                        debitResp.Trx_id = TransId;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        debitResp.Error_code = -4;
                        debitResp.Error_msg = "Insufficient balance";
                    }
                    else
                    {
                        debitResp.Error_code = 1;
                        debitResp.Error_msg = "Transaction already exists";
                    }
                }
                catch (Exception ex)
                {
                    debitResp.Error_code = -1;
                    debitResp.Error_msg = "Unknown error";
                    log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(debitResp)}\r\n{ex}", "GPI_debit");
                }
            }

            string debitLog = $"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(debitResp)}";
            log2File.Info(debitLog, "GPI_debit");
            return debitResp;
        }

        public class CreditReq : SettledDataRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class CreditResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
            [XmlElement(ElementName = "before")]
            public decimal Before { get; set; }
            [XmlElement(ElementName = "after")]
            public decimal After { get; set; }
            [XmlElement(ElementName = "exchange_rate")]
            public decimal Exchange_rate { get; set; }
            [XmlElement(ElementName = "trx_id")]
            public string Trx_id { get; set; }
        }

        [Route("[controller]/ms/credit")]
        [HttpPost]
        [Produces("application/xml")]
        public CreditResp credit([FromForm] CreditReq creditReq)
        {
            CreditResp creditResp = new CreditResp();
            creditResp.Exchange_rate = 1;
            creditResp.Error_code = -1;
            //CreditReq creditReq = JsonConvert.DeserializeObject<CreditReq>(@" {""merch_id"":""NVCASH"",""merch_pwd"":""D9988E76-BFC9-42D8-91AC-26DD98B624C9"",""cust_id"":""DNova88_9"",""currency"":""VND"",""amount"":0.0,""trx_id"":""5392eb41-2f83-4395-aea8-2fa85e93e0ae"",""Operator"":null,""game_id"":""200018"",""round_id"":""114165222"",""bundle_id"":null,""jackpot_win"":0.0,""platform"":""1"",""rake"":null,""rakedhands"":null}");
            //logger.LogCritical(JsonConvert.SerializeObject(creditReq));
            try
            {
                int result = settledBLL.PayoutRNG_GPI(creditReq, out decimal tpAffterAmount, out string TransId);

                if (result > 0)
                {
                    creditResp.Trx_id = TransId;
                    if (creditReq.amount > 0)
                    {
                        creditResp.After = tpAffterAmount;
                        creditResp.Before = tpAffterAmount - creditReq.amount;
                        creditResp.Error_code = 0;
                        creditResp.Error_msg = "No errors";
                    }
                    else if (creditReq.amount == 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.GPI, creditReq.cust_id, gameType);
                        creditResp.After = tpCustBalance.Balance;
                        creditResp.Before = tpCustBalance.Balance;
                        creditResp.Error_code = 0;
                        creditResp.Error_msg = "No errors";
                    }
                }
                else if (result == -1)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.GPI, creditReq.cust_id, gameType);
                    creditResp.After = tpCustBalance.Balance;
                    creditResp.Before = tpCustBalance.Balance;
                    creditResp.Error_code = 0;
                    creditResp.Error_msg = "No errors";
                }
            }
            catch (Exception ex)
            {
                creditResp.Error_code = -1;
                creditResp.Error_msg = "Unknown error";
                log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}\r\n{ex}", "GPI_credit");
                throw ex;
            }

            string creditLog = $"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(creditResp)}";
            log2File.Info(creditLog, "GPI_credit");
            return creditResp;
        }
        public class EndgameReq : EndgameRNG_GPI
        {
            public string merch_id { get; set; }
            public string merch_pwd { get; set; }
        }

        [XmlRoot(ElementName = "resp")]
        public class EndgameResp
        {
            [XmlElement(ElementName = "error_code")]
            public int Error_code { get; set; }
            [XmlElement(ElementName = "error_msg")]
            public string Error_msg { get; set; }
        }

        [Route("[controller]/ms/endgame")]
        [HttpPost]
        [Produces("application/xml")]
        public EndgameResp endgame([FromForm] EndgameReq endgameReq)
        {
            EndgameResp endgameResp = new EndgameResp();
            endgameResp.Error_code = -1;
            endgameResp.Error_msg = "Unknown error";

            int result = settledBLL.SetSettleFlagRNG_GPI(endgameReq.round_id, endgameReq.cust_id, 2);
            endgameResp.Error_code = 0;
            endgameResp.Error_msg = "No errors";

            string endgameLog = $"{JsonConvert.SerializeObject(endgameReq)}\r\n{JsonConvert.SerializeObject(endgameResp)}";
            log2File.Info(endgameLog, "GPI_endgame");

            return endgameResp;
        }
    }
}