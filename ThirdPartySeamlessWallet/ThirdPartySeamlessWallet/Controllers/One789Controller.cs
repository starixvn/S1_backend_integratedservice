using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly Log2File _log2File;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tp _thisTp;

        public One789Controller(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _thisTp = Tp.One789;
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
            string logName = $"{_thisTp}_balance";
            Response.StatusCode = (int)Error.INVALID_DATA;
            ApiResponse getApiResponse = new ApiResponse();
            GetBalanceResp getBalanceResp = new GetBalanceResp();

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, username);
            if (tpCheck == null)
            {
                getApiResponse.ErrorCode = "ValidationException";
                getApiResponse.ErrorMessage = "Invalid user Id";
                _log2File.Error($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            if (tpCheck.Locked)
            {
                getApiResponse.ErrorCode = "ValidationException";
                getApiResponse.ErrorMessage = "Account is Lock";
                _log2File.Error($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, $"{production}_");
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, username, (int)gameType);
            if (tpCustBalance != null)
            {
                Response.StatusCode = (int)Error.OK;
                getBalanceResp.Balance = tpCustBalance.Balance;
            }

            _log2File.Info($"account={username}&production={production}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
            return getBalanceResp;
        }

        [HttpPost]
        public async Task<dynamic> balance_transactionsAsync()
        {
            string logName = $"{_thisTp}_balance_transactions";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            BettingDataLT_One789 getReq = JsonConvert.DeserializeObject<BettingDataLT_One789>(Content);
            _log2File.Info(Content, logName);

            Response.StatusCode = (int)Error.INVALID_DATA;
            ApiResponse getApiResponse = new()
            {
                ErrorCode = "ApiException",
                ErrorMessage = "General exception"
            };
            GetBalanceTransactionsResp getBalanceTransactionsResp = new();

            GameType gameType = 0;
            var result = 0;
            logName = $"{_thisTp}_balance_transactions_{getReq.Type}";
            switch (getReq.Type)
            {
                case "payout":
                    gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getReq.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    result = _settledBLL.PayoutLT_One789(getReq, (int)gameType, out decimal tpAffterAmount);
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
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.Username, (int)gameType);
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                    }
                    break;
                case "bet":
                    gameType = _thirdPartyBLL.GetGameType((int)_thisTp, getReq.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.Username);
                    if (tpCheck == null)
                    {
                        getApiResponse.ErrorCode = "ValidationException";
                        getApiResponse.ErrorMessage = "Invalid user Id";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    if (tpCheck.Locked)
                    {
                        getApiResponse.ErrorCode = "ValidationException";
                        getApiResponse.ErrorMessage = "Account is Lock";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    try
                    {
                        result = _bettingBLL.PlaceBetLT_One789(getReq, (int)gameType, out tpAffterAmount);
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
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.Username, (int)gameType);
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
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                        return getApiResponse;
                    }
                    break;
                case "payout_cancel":
                    var payoutBettrans = _settledBLL.GetRecordLT_One789(getReq.Source);
                    if (payoutBettrans.Source == null)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    gameType = _thirdPartyBLL.GetGameType((int)_thisTp, payoutBettrans.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    getReq.RefSource = payoutBettrans.Source;
                    result = _settledBLL.ResettleLT_One789(getReq, (int)gameType, out tpAffterAmount);
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
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.Username, (int)gameType);
                        getBalanceTransactionsResp.Tx = getReq.Tx;
                        getBalanceTransactionsResp.Amount = getReq.Amount;
                        getBalanceTransactionsResp.Balance = tpCustBalance.Balance;
                    }
                    break;
                case "refund":
                    var refundBettrans = _settledBLL.GetRecordLT_One789(getReq.Source);
                    if (refundBettrans.Source == null)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    gameType = _thirdPartyBLL.GetGameType((int)_thisTp, refundBettrans.Source);
                    if (gameType == 0)
                    {
                        getApiResponse.ErrorMessage = "General exception (gameType error)";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                        return getApiResponse;
                    }
                    getReq.RefSource = refundBettrans.Source;
                    result = _settledBLL.RefundLT_One789(getReq, (int)gameType, out tpAffterAmount);
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
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getReq.Username, (int)gameType);
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
                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceTransactionsResp)}", logName);
                return getBalanceTransactionsResp;
            }
            else
            {
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
        }
    }
}
