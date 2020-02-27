using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AGController : ControllerBase
    {
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SAController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Dictionary<string, string> BetTransactionCode = new Dictionary<string, string>()
        {
            {"SWB", null},
            {"BCB", null},
            {"DTB", null},
            {"RLB", null},
            {"SBB", null},
            {"CBB", null},
            {"LBB", null},
            {"NNB", null},
            {"ZJB", null},
            {"BJB", null},
        };


        public AGController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
        }
        [XmlRoot("Record")]
        public class Record
        {
            [XmlElement(ElementName = "sessionToken")]
            public string SessionToken { get; set; }
            [XmlElement(ElementName = "currency")]
            public string Currency { get; set; }
            [XmlElement(ElementName = "playname")]
            public string Playname { get; set; }
            [XmlElement(ElementName = "agentCode")]
            public string AgentCode { get; set; }
            [XmlElement(ElementName = "transactionID")]
            public string Round { get; set; }
            [XmlElement(ElementName = "gametype")]
            public string Gametype { get; set; }
            [XmlElement(ElementName = "gameCode")]
            public string GameCode { get; set; }
            [XmlElement(ElementName = "tableCode")]
            public string TableCode { get; set; }
            [XmlElement(ElementName = "transactionType")]
            public string TransactionType { get; set; }
            [XmlElement(ElementName = "transactionCode")]
            public string TransactionCode { get; set; }
        }

        [XmlRoot(ElementName = "Data")]
        public class Data
        {
            [XmlElement(ElementName = "Record")]
            public Record Record { get; set; }
        }

        [XmlRoot("Record")]
        public class BetRecord
        {
            [XmlElement(ElementName = "sessionToken")]
            public string SessionToken { get; set; }
            [XmlElement(ElementName = "currency")]
            public string Currency { get; set; }
            [XmlElement(ElementName = "value")]
            public string Value { get; set; }
            [XmlElement(ElementName = "playname")]
            public string Playname { get; set; }
            [XmlElement(ElementName = "agentCode")]
            public string AgentCode { get; set; }
            [XmlElement(ElementName = "betTime")]
            public string BetTime { get; set; }
            [XmlElement(ElementName = "transactionID")]
            public string TransactionID { get; set; }
            [XmlElement(ElementName = "platformType")]
            public string PlatformType { get; set; }
            [XmlElement(ElementName = "round")]
            public string Round { get; set; }
            [XmlElement(ElementName = "gametype")]
            public string Gametype { get; set; }
            [XmlElement(ElementName = "gameCode")]
            public string GameCode { get; set; }
            [XmlElement(ElementName = "tableCode")]
            public string TableCode { get; set; }
            [XmlElement(ElementName = "transactionType")]
            public string TransactionType { get; set; }
            [XmlElement(ElementName = "transactionCode")]
            public string TransactionCode { get; set; }
            [XmlElement(ElementName = "deviceType")]
            public string DeviceType { get; set; }
            [XmlElement(ElementName = "playtype")]
            public string Playtype { get; set; }
        }

        [XmlRoot(ElementName = "Data")]
        public class BetData
        {
            [XmlElement(ElementName = "Record")]
            public BetRecord Record { get; set; }
        }

        [XmlRoot(ElementName = "Record")]
        public class PayoutRecord
        {
            [XmlElement(ElementName = "sessionToken")]
            public string SessionToken { get; set; }
            [XmlElement(ElementName = "currency")]
            public string Currency { get; set; }
            [XmlElement(ElementName = "netAmount")]
            public string NetAmount { get; set; }
            [XmlElement(ElementName = "validBetAmount")]
            public string ValidBetAmount { get; set; }
            [XmlElement(ElementName = "playname")]
            public string Playname { get; set; }
            [XmlElement(ElementName = "agentCode")]
            public string AgentCode { get; set; }
            [XmlElement(ElementName = "settletime")]
            public string Settletime { get; set; }
            [XmlElement(ElementName = "billNo")]
            public string BillNo { get; set; }
            [XmlElement(ElementName = "transactionID")]
            public string TransactionID { get; set; }
            [XmlElement(ElementName = "gametype")]
            public string Gametype { get; set; }
            [XmlElement(ElementName = "gameCode")]
            public string GameCode { get; set; }
            [XmlElement(ElementName = "transactionType")]
            public string TransactionType { get; set; }
            [XmlElement(ElementName = "transactionCode")]
            public string TransactionCode { get; set; }
            [XmlElement(ElementName = "playtype")]
            public string Playtype { get; set; }
            [XmlElement(ElementName = "ticketStatus")]
            public string TicketStatus { get; set; }
            [XmlElement(ElementName = "gameResult")]
            public string GameResult { get; set; }
            [XmlElement(ElementName = "finish")]
            public string Finish { get; set; }
        }

        [XmlRoot(ElementName = "Data")]
        public class PayoutData
        {
            [XmlElement(ElementName = "Record")]
            public PayoutRecord Record { get; set; }
        }
        [XmlRoot(ElementName = "Record")]
        public class RefundRecord
        {
            [XmlElement(ElementName = "ticketStatus")]
            public string TicketStatus { get; set; }
            [XmlElement(ElementName = "sessionToken")]
            public string SessionToken { get; set; }
            [XmlElement(ElementName = "currency")]
            public string Currency { get; set; }
            [XmlElement(ElementName = "value")]
            public string Value { get; set; }
            [XmlElement(ElementName = "playname")]
            public string Playname { get; set; }
            [XmlElement(ElementName = "agentCode")]
            public string AgentCode { get; set; }
            [XmlElement(ElementName = "betTime")]
            public string BetTime { get; set; }
            [XmlElement(ElementName = "transactionID")]
            public string TransactionID { get; set; }
            [XmlElement(ElementName = "billNo")]
            public string BillNo { get; set; }
            [XmlElement(ElementName = "platformType")]
            public string PlatformType { get; set; }
            [XmlElement(ElementName = "round")]
            public string Round { get; set; }
            [XmlElement(ElementName = "gametype")]
            public string Gametype { get; set; }
            [XmlElement(ElementName = "gameCode")]
            public string GameCode { get; set; }
            [XmlElement(ElementName = "tableCode")]
            public string TableCode { get; set; }
            [XmlElement(ElementName = "transactionType")]
            public string TransactionType { get; set; }
            [XmlElement(ElementName = "transactionCode")]
            public string TransactionCode { get; set; }
            [XmlElement(ElementName = "playtype")]
            public string Playtype { get; set; }
        }

        [XmlRoot(ElementName = "Data")]
        public class RefundData
        {
            [XmlElement(ElementName = "Record")]
            public Record Record { get; set; }
        }

        public class TransferResponse
        {
            public TransferResponse()
            {
                ResponseCode = Error.ERROR.ToString();
            }
            public string ResponseCode { get; set; }
            public decimal Balance { get; set; }
        }

        [HttpPost]
        [Produces("application/xml")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<TransferResponse> postTransferAsync()
        {
            string Content = "";
            var resp = new TransferResponse();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                //logger.LogInformation(Content);
                var xml = tools.XMLDeserialize<Data>(Content);
                var dataLC_AG = JsonConvert.DeserializeObject<DataLC_AG>(JsonConvert.SerializeObject(xml.Record));
                string username = dataLC_AG.Playname.Substring(dataLC_AG.AgentCode.Length);

                if (dataLC_AG.TransactionType.Equals("BET", StringComparison.OrdinalIgnoreCase))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.AG, username);
                    if (tpCheck == null || tpCheck.Token != dataLC_AG.SessionToken)
                    {
                        resp.ResponseCode = Error.INVALID_SESSION.ToString();
                        Response.StatusCode = (int)Error.INVALID_SESSION;
                        return resp;
                    }
                    else if (tpCheck.Locked)
                    {
                        resp.ResponseCode = Error.INSUFFICIENT_CLEARED_FUNDS.ToString();
                        Response.StatusCode = (int)Error.INSUFFICIENT_CLEARED_FUNDS;
                        return resp;
                    }
                    var xmlBet = tools.XMLDeserialize<BetData>(Content);
                    var bettingDataLC_AG = JsonConvert.DeserializeObject<BettingDataLC_AG>(JsonConvert.SerializeObject(xmlBet.Record));

                    decimal tpAffterAmount = 0;
                    if (bettingDataLC_AG.Value > 0)
                    {
                        logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_AG));
                        if (bettingBLL.PlaceBet((int)Tp.AG, bettingDataLC_AG.TransactionID.ToString(), username, bettingDataLC_AG.Value, GameType.AG_LiveCasino, JsonConvert.SerializeObject(bettingDataLC_AG), out tpAffterAmount) > 0)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            resp.Balance = tpAffterAmount;
                        }
                        else if (tpAffterAmount < 0)
                        {
                            resp.ResponseCode = Error.INSUFFICIENT_FUNDS.ToString();
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                    }
                }
                else if (dataLC_AG.TransactionType.Equals("LOSE", StringComparison.OrdinalIgnoreCase) || dataLC_AG.TransactionType.Equals("WIN", StringComparison.OrdinalIgnoreCase))
                {
                    var xmlPayout = tools.XMLDeserialize<PayoutData>(Content);
                    var payoutDataLC_AG = JsonConvert.DeserializeObject<PayoutDataLC_AG>(JsonConvert.SerializeObject(xmlPayout.Record));
                    logger.LogInformation(JsonConvert.SerializeObject(payoutDataLC_AG));
                    decimal tpAffterAmount = 0;
                    var reslut = settledBLL.SettledLC_AG(payoutDataLC_AG, out tpAffterAmount);

                    if (reslut > 0)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.AG, username);
                        resp.Balance = tpCustBalance.Balance;
                    }
                    else if (reslut == -1)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.AG, username);
                        resp.Balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                    }
                }
                else if (dataLC_AG.TransactionType.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
                {
                    decimal tpAffterAmount = 0;
                    var xmlRefund = tools.XMLDeserialize<RefundData>(Content);
                    var bettingDataLC_AG_Refund = JsonConvert.DeserializeObject<BettingDataLC_AG_Refund>(JsonConvert.SerializeObject(xmlRefund.Record));
                    logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_AG_Refund));
                    if (bettingDataLC_AG_Refund.Value > 0)
                    {
                        if (settledBLL.Refund((int)Tp.AG, bettingDataLC_AG_Refund.TransactionID.ToString(), username, bettingDataLC_AG_Refund.Value, 0, GameType.AG_LiveCasino, out tpAffterAmount) > 0)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            resp.Balance = tpAffterAmount;
                            //throw new NotImplementedException();
                        }
                        else if (tpAffterAmount < 0)
                        {
                            resp.ResponseCode = Error.INSUFFICIENT_FUNDS.ToString();
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                    }
                }

                if (resp.ResponseCode != Error.OK.ToString())
                {
                    Error error = Error.ERROR;
                    Enum.TryParse(resp.ResponseCode, out error);
                    Response.StatusCode = (int)error;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                resp.ResponseCode = Error.ERROR.ToString();
                Response.StatusCode = (int)Error.ERROR;
            }
            return resp;
        }

        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            INCORRECT_SESSION_TYPE = 403,
            INVALID_SESSION = 404,
            INVALID_TRANSACTION = 404,
            INSUFFICIENT_FUNDS = 409,
            INSUFFICIENT_CLEARED_FUNDS = 409,
            ERROR = 500,
        }
    }
}