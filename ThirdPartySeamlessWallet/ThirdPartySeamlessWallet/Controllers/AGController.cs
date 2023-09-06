using System.Text;
using Microsoft.AspNetCore.Mvc;
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
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<AGController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly int _thisTp;
        private readonly int _gameType = 0;
        private readonly string _tpName = Tp.AG.ToString();

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

        public AGController(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<AGController>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = (int)Tp.AG;
            _gameType = (int)GameType.AG_LiveCasino;
        }

        #region Request

        public class BalanceReq
        {
            public string TpUserName { get; set; }
        }

        #endregion

        #region Response

        public class TransferResponse
        {
            public TransferResponse()
            {
                ResponseCode = Error.ERROR.ToString();
            }
            public string ResponseCode { get; set; }
            public decimal Balance { get; set; }
        }

        #endregion

        [HttpPost]
        [Produces("application/xml")]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<TransferResponse> postTransferAsync()
        {
            string logName = $"{_tpName}_PostTransfer";
            string Content = "";
            var resp = new TransferResponse();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var xml = _tools.XMLDeserialize<Data>(Content);
                var dataLC_AG = JsonConvert.DeserializeObject<DataLC_AG>(JsonConvert.SerializeObject(xml.Record));
                string username = dataLC_AG.Playname.Substring(dataLC_AG.AgentCode.Length);

                if (dataLC_AG.TransactionType.Equals("BET", StringComparison.OrdinalIgnoreCase))
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, username);
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
                    var xmlBet = _tools.XMLDeserialize<BetData>(Content);
                    var bettingDataLC_AG = JsonConvert.DeserializeObject<BettingDataLC_AG>(JsonConvert.SerializeObject(xmlBet.Record));

                    decimal tpAffterAmount = 0;
                    if (bettingDataLC_AG.Value > 0)
                    {
                        _logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_AG));
                        int result = _bettingBLL.PlaceBetLC_AG(bettingDataLC_AG, out tpAffterAmount);
                        if (result > 0)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            resp.Balance = tpAffterAmount;
                        }
                        else if (tpAffterAmount < 0)
                        {
                            resp.ResponseCode = Error.INSUFFICIENT_FUNDS.ToString();
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                            resp.Balance = tpCustBalance.Balance;
                        }
                        else if (result == -1)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, username, _gameType);
                            resp.Balance = tpCustBalance.Balance;
                            _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{logName}_Retry");
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                            resp.Balance = tpCustBalance.Balance;
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                        resp.Balance = tpCustBalance.Balance;
                    }
                }
                else if (dataLC_AG.TransactionType.Equals("LOSE", StringComparison.OrdinalIgnoreCase) || dataLC_AG.TransactionType.Equals("WIN", StringComparison.OrdinalIgnoreCase))
                {
                    var xmlPayout = _tools.XMLDeserialize<PayoutData>(Content);
                    var payoutDataLC_AG = JsonConvert.DeserializeObject<PayoutDataLC_AG>(JsonConvert.SerializeObject(xmlPayout.Record));
                    int reslut = 0;
                    payoutDataLC_AG.TransactionID = xmlPayout.Record.TransactionIDs[0];

                    _logger.LogInformation(JsonConvert.SerializeObject(payoutDataLC_AG));
                    decimal tpAffterAmount = 0;
                    reslut = _settledBLL.PayoutLC_AG(payoutDataLC_AG, out tpAffterAmount);

                    if (reslut > 0)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, username, _gameType);
                        resp.Balance = tpCustBalance.Balance;
                    }
                    else if (reslut == -1)
                    {
                        resp.ResponseCode = Error.OK.ToString();
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
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
                    var xmlRefund = _tools.XMLDeserialize<RefundData>(Content);
                    var bettingDataLC_AG_Refund = JsonConvert.DeserializeObject<BettingDataLC_AG_Refund>(JsonConvert.SerializeObject(xmlRefund.Record));
                    _logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_AG_Refund));
                    if (bettingDataLC_AG_Refund.Value > 0)
                    {
                        var result = _settledBLL.RefundLC_AG(bettingDataLC_AG_Refund, out tpAffterAmount);

                        if (result > 0)
                        {
                            resp.ResponseCode = Error.OK.ToString();
                            resp.Balance = tpAffterAmount;
                            //throw new NotImplementedException();
                        }
                        else if (tpAffterAmount < 0)
                        {
                            resp.ResponseCode = Error.INSUFFICIENT_FUNDS.ToString();
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                            resp.Balance = tpCustBalance.Balance;
                        }
                        else
                        {
                            resp.ResponseCode = Error.INVALID_DATA.ToString();
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                            resp.Balance = tpCustBalance.Balance;
                        }
                    }
                    else
                    {
                        resp.ResponseCode = Error.INVALID_DATA.ToString();
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
                        resp.Balance = tpCustBalance.Balance;
                    }
                }

                if (resp.ResponseCode != Error.OK.ToString())
                {
                    Error error = Error.ERROR;
                    Enum.TryParse(resp.ResponseCode, out error);
                    Response.StatusCode = (int)error;
                }

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{logName}_{dataLC_AG.TransactionType}");
            }
            catch (Exception ex)
            {
                resp.ResponseCode = Error.ERROR.ToString();
                Response.StatusCode = (int)Error.ERROR;
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
            }

            return resp;
        }

        [HttpPost]
        public async Task<TransferResponse> GetBalanceAsync()
        {
            // 給TPS取得額度登入時帶入
            string logName = $"{_tpName}_GetBalance";
            string Content = "";
            var resp = new TransferResponse();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var getReq = JsonConvert.DeserializeObject<BalanceReq>(Content);

                // 檢查會員是否存在
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfo(_thisTp, getReq.TpUserName);
                if (tpCustInfo == null)
                {
                    resp.ResponseCode = Error.INVALID_DATA.ToString();
                    resp.Balance = 0;
                    _log2File.Error($"{JsonConvert.SerializeObject(getReq)}", logName);
                    return resp;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.TpUserName, _gameType);
                resp.ResponseCode = Error.OK.ToString();
                resp.Balance = tpCustBalance.Balance;
                return resp;
            }
            catch (Exception ex)
            {
                resp.ResponseCode = Error.ERROR.ToString();
                Response.StatusCode = (int)Error.ERROR;
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
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