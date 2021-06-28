using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class One789Controller : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tp thisTp;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly IHostEnvironment environment;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        //private readonly int gameType = 0;

        public One789Controller(
            IHostEnvironment environment,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            AuthBLL authBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL
        )
        {
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.environment = environment;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            thisTp = Tp.One789;
        }
        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            ERROR = 500,
        }

        #region Request

        #endregion

        #region Response

        public class ApiResponse
        {
            [JsonPropertyName("ErrorCode")]
            public string ErrorCode { get; set; }
            [JsonPropertyName("ErrorMessage")]
            public string ErrorMessage { get; set; }
        }

        public class GetBalanceResp
        {
            [JsonPropertyName("Balance")]
            public decimal Balance { get; set; }
        }

        public class GetBalanceTransactionsResp
        {
            [JsonPropertyName("Tx")]
            public string Tx { get; set; }
            [JsonPropertyName("Amount")]
            public decimal Amount { get; set; }
            [JsonPropertyName("Balance")]
            public decimal Balance { get; set; }
        }

        #endregion

        [HttpGet]
        public dynamic balance(string username, string production)
        {
            string logName = $"{thisTp}_balance";
            Response.StatusCode = (int)Error.INVALID_DATA;
            ApiResponse getApiResponse = new ApiResponse();
            GetBalanceResp getBalanceResp = new GetBalanceResp();

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, username);
            if (tpCheck == null)
            {
                getApiResponse.ErrorCode = "ValidationException";
                getApiResponse.ErrorMessage = "Invalid user Id";
                log2File.Info($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            if (tpCheck.Locked)
            {
                getApiResponse.ErrorCode = "ValidationException";
                getApiResponse.ErrorMessage = "Account is Lock";
                log2File.Info($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, username);
            var gameType = thirdPartyBLL.GetGameType((int)thisTp, $"{production}_");
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, username, (int)gameType);
            if (tpCustBalance != null)
            {
                Response.StatusCode = (int)Error.OK;
                getBalanceResp.Balance = tpCustBalance.Balance;
            }

            log2File.Info($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
            return getBalanceResp;
        }

        [HttpPost]
        public async Task<dynamic> balance_transactionsAsync()
        {
            string logName = $"{thisTp}_balance_transactions";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            BettingDataLT_One789 getReq = JsonConvert.DeserializeObject<BettingDataLT_One789>(Content);
            log2File.Info(Content, logName);
            Response.StatusCode = (int)Error.INVALID_DATA;
            ApiResponse getApiResponse = new ApiResponse();
            getApiResponse.ErrorCode = "ApiException";
            getApiResponse.ErrorMessage = "General exception";
            GetBalanceTransactionsResp getBalanceTransactionsResp = new GetBalanceTransactionsResp();

            GameType gameType = 0;
            var result = 0;
            logName = $"{thisTp}_balance_transactions_{getReq.Type}";
            switch (getReq.Type)
            {
                case "payout":
                    gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    result = settledBLL.PayoutLT_One789(getReq, (int)gameType, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        Response.StatusCode = (int)Error.OK;
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpAffterAmount;
                    }
                    else if (result == -1)
                    {
                        Response.StatusCode = (int)Error.OK;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.Username, (int)gameType);
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                    }

                    break;
                case "bet":
                    gameType = thirdPartyBLL.GetGameType((int)thisTp, getReq.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.Username);
                    if (tpCheck == null)
                    {
                        getApiResponse.ErrorCode = "ValidationException";
                        getApiResponse.ErrorMessage = "Invalid user Id";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    if (tpCheck.Locked)
                    {
                        getApiResponse.ErrorCode = "ValidationException";
                        getApiResponse.ErrorMessage = "Account is Lock";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    try
                    {
                        result = bettingBLL.PlaceBetLT_One789(getReq, (int)gameType, out tpAffterAmount);
                        if (result > 0)
                        {
                            Response.StatusCode = (int)Error.OK;
                            getBalanceTransactionsResp.Tx = getReq.Tx;
                            getBalanceTransactionsResp.Amount = getReq.Amount;
                            getBalanceTransactionsResp.Balance = tpAffterAmount;
                        }
                        else if (result == -1)
                        {
                            Response.StatusCode = (int)Error.OK;
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.Username, (int)gameType);
                            getBalanceTransactionsResp.Tx = getReq.Tx;
                            getBalanceTransactionsResp.Amount = getReq.Amount;
                            getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                        }
                        else if (tpAffterAmount < 0)
                        {
                            getApiResponse.ErrorCode = "BalanceInsufficientException";
                            getApiResponse.ErrorMessage = "Account balance insufficient";
                        }
                    }
                    catch (Exception ex)
                    {
                        Response.StatusCode = (int)Error.ERROR;
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                        return getApiResponse;
                    }
                    break;
                case "payout_cancel":
                    var payoutBettrans = settledBLL.GetRecordLT_One789(getReq.Source);
                    if (payoutBettrans.Source == null)
                    {
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    gameType = thirdPartyBLL.GetGameType((int)thisTp, payoutBettrans.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    getReq.RefSource = payoutBettrans.Source;
                    result = settledBLL.ResettleLT_One789(getReq, (int)gameType, out tpAffterAmount);
                    if (result > 0)
                    {
                        Response.StatusCode = (int)Error.OK;
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpAffterAmount;
                    }
                    else if (result == -1)
                    {
                        Response.StatusCode = (int)Error.OK;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.Username, (int)gameType);
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                    }
                    break;
                case "refund":
                    var refundBettrans = settledBLL.GetRecordLT_One789(getReq.Source);
                    if (refundBettrans.Source == null)
                    {
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    gameType = thirdPartyBLL.GetGameType((int)thisTp, refundBettrans.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    getReq.RefSource = refundBettrans.Source;
                    result = settledBLL.RefundLT_One789(getReq, (int)gameType, out tpAffterAmount);
                    if (result > 0)
                    {
                        Response.StatusCode = (int)Error.OK;
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpAffterAmount;
                    }
                    else if (result == -1)
                    {
                        Response.StatusCode = (int)Error.OK;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.Username, (int)gameType);
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                    }
                    break;
                default:
                    break;
            }

            if (Response.StatusCode == (int)Error.OK)
            {
                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceTransactionsResp)}", logName);
                return getBalanceTransactionsResp;
            }
            else
            {
                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
        }
    }
}
