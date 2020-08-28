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
        private readonly Log2File log2File;
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


        public AGController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
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
                        int result = bettingBLL.PlaceBetLC_AG(bettingDataLC_AG, out tpAffterAmount);
                        if (result > 0)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            resp.Balance = tpAffterAmount;
                        }
                        else if (tpAffterAmount < 0)
                        {
                            resp.ResponseCode = Error.INSUFFICIENT_FUNDS.ToString();
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                            resp.Balance = tpCustBalance.Balance;
                        }
                        else if (result == -1)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.AG, username);
                            resp.Balance = tpCustBalance.Balance;
                            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", "AG_PostTransfer_Retry");
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                            resp.Balance = tpCustBalance.Balance;
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                        resp.Balance = tpCustBalance.Balance;
                    }
                }
                else if (dataLC_AG.TransactionType.Equals("LOSE", StringComparison.OrdinalIgnoreCase) || dataLC_AG.TransactionType.Equals("WIN", StringComparison.OrdinalIgnoreCase))
                {
                    var xmlPayout = tools.XMLDeserialize<PayoutData>(Content);
                    var payoutDataLC_AG = JsonConvert.DeserializeObject<PayoutDataLC_AG>(JsonConvert.SerializeObject(xmlPayout.Record));
                    int reslut = 0;

                    if (xmlPayout.Record.TransactionIDs.Count > 1)
                    {
                        //payoutDataLC_AG.Memo = string.Join(",", xmlPayout.Record.TransactionIDs);
                        //reslut = settledBLL.SettledLC_AG_Merger(payoutDataLC_AG, JsonConvert.SerializeObject(xmlPayout.Record.TransactionIDs.Where(x => x != payoutDataLC_AG.TransactionID)));
                    }
                    else
                    {
                        payoutDataLC_AG.TransactionID = xmlPayout.Record.TransactionIDs[0];
                    }

                    logger.LogInformation(JsonConvert.SerializeObject(payoutDataLC_AG));
                    decimal tpAffterAmount = 0;
                    reslut = settledBLL.SettledLC_AG(payoutDataLC_AG, out tpAffterAmount);

                    if (reslut > 0)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.AG, username);
                        resp.Balance = tpCustBalance.Balance;
                    }
                    else if (reslut == -1)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
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
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                            resp.Balance = tpCustBalance.Balance;
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                            resp.Balance = tpCustBalance.Balance;
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.AG, username);
                        resp.Balance = tpCustBalance.Balance;
                    }
                }

                if (resp.ResponseCode != Error.OK.ToString())
                {
                    Error error = Error.ERROR;
                    Enum.TryParse(resp.ResponseCode, out error);
                    Response.StatusCode = (int)error;
                }

                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", "AG_PostTransfer");
            }
            catch (Exception ex)
            {
                //logger.LogError(ex.Message);
                resp.ResponseCode = Error.ERROR.ToString();
                Response.StatusCode = (int)Error.ERROR;
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "AG_PostTransfer");
            }

            //logger.LogCritical(JsonConvert.SerializeObject(resp));
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