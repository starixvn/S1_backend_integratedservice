using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class IMController : ControllerBase
    {
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly IIMBLL _imBll;
        private readonly Log2File _log2File;
        private readonly string _tpGameName;

        public IMController(IServiceProvider provider)
        {
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _imBll = provider.GetRequiredService<IIMBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _tpGameName = Tp.IM.ToString();
        }

        Dictionary<string, Tp> _productWalletToTpId = new()
        {
            { "IMSportsbook", Tp.IM },
            { "IMESports", Tp.IM },
            { "IMSlot", Tp.SPRIBE },
        };

        Dictionary<string, GameType> _productWalletToTpGameType = new()
        {
            { "IMSportsbook", GameType.IM_Sports },
            { "IMESports", GameType.IM_ESports },
            { "IMSlot", GameType.SPRIBE_RNG },
        };

        #region Request

        public class GetBalanceReq
        {
            public string ProductWallet { get; set; }
            public string PlayerId { get; set; }
            public string Currency { get; set; }
        }

        #endregion

        #region Response

        public class GetBalanceResp
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public string PlayerId { get; set; }
            public string Currency { get; set; }
            public decimal Balance { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/GetBalance")]
        public GetBalanceResp GetBalance(GetBalanceReq req)
        {
            var logName = $"{_tpGameName}_GetBalance";
            var checkPlayerResp = new GetBalanceResp();

            try
            {
                // check tp
                var tpId = GetTpId(req.ProductWallet);
                var tpGameType = GetTpGameType(req.ProductWallet);
                if (tpId == Tp.None || tpGameType == GameType.None)
                {
                    checkPlayerResp.Code = (int)IMResultCode.InvalidArgument;
                    checkPlayerResp.Message = "Product Wallet Invalid";
                    _log2File.Error($"{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                    return checkPlayerResp;
                }

                // check user (exist status online)
                var tpUserName = req.PlayerId;
                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)tpId, tpUserName);
                if (tpCheck == null)
                {
                    checkPlayerResp.Code = (int)IMResultCode.PlayerDoesNotExist;
                    checkPlayerResp.Message = "Player Does Not Exist";
                    _log2File.Error($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                    return checkPlayerResp;
                }
                else if (tpCheck.Locked)
                {
                    checkPlayerResp.Code = (int)IMResultCode.PlayerIsInactive;
                    checkPlayerResp.Message = "Player Is Inactive";
                    _log2File.Error($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                    return checkPlayerResp;
                }

                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)tpId, tpUserName, (int)tpGameType);
                checkPlayerResp.Code = (int)IMResultCode.Success;
                checkPlayerResp.Message = "Successful";
                checkPlayerResp.PlayerId = req.PlayerId;
                checkPlayerResp.Currency = req.Currency;
                checkPlayerResp.Balance = tpCustBalance.Balance;
                return checkPlayerResp;
            }
            catch (Exception e)
            {
                checkPlayerResp.Code = (int)IMResultCode.Exception;
                checkPlayerResp.Message = "Unknown Error";
                _log2File.Error($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName, e);
                return checkPlayerResp;
            }
        }

        [HttpPost]
        [Route("[controller]/PlaceBet")]
        public async Task<dynamic> PlaceBet()
        {
            var logName = $"{_tpGameName}_PlaceBet";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var getErrorResponse = new ErrorRespIM
            {
                Code = (int)IMResultCode.Exception,
                Message = "Error"
            };

            try
            {
                var getReq = JsonConvert.DeserializeObject<BettingRequest_IM>(content);

                // check tp
                var tpId = GetTpId(getReq.ProductWallet);
                var tpGameType = GetTpGameType(getReq.ProductWallet);
                if (tpId == Tp.None || tpGameType == GameType.None)
                {
                    getErrorResponse.Code = (int)IMResultCode.InvalidArgument;
                    getErrorResponse.Message = "Product Wallet Invalid (TP)";
                    _log2File.Error($"{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var transactionsList = getReq.Transactions;

                var tpUserName = transactionsList.FirstOrDefault().PlayerId;
                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)tpId, tpUserName);
                if (tpCheck == null)
                {
                    getErrorResponse.Code = (int)IMResultCode.PlayerDoesNotExist;
                    getErrorResponse.Message = "Player Does Not Exist";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                else if (tpCheck.Locked)
                {
                    getErrorResponse.Code = (int)IMResultCode.PlayerIsInactive;
                    getErrorResponse.Message = "Player Is Inactive";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var custBalance = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)tpGameType);
                var betSum = transactionsList.Sum(x => x.Amount);
                var checkBalance = custBalance.Balance - betSum;
                if (checkBalance < 0)
                {
                    getErrorResponse.Code = (int)IMResultCode.NotEnoughBalance;
                    getErrorResponse.Message = "Not enough balance";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                var result = _imBll.Bet_IM(transactionsList, tpUserName, tpId, tpGameType, out var outData);

                if (result > 0)
                {
                    var response = new BetResponse
                    {
                        Results = outData
                    };
                    _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                    return response;
                }
                else if (result == -1)
                {
                    getErrorResponse.Code = (int)IMResultCode.NotEnoughBalance;
                    getErrorResponse.Message = "Not enough balance";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

                return getErrorResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/SettleBet")]
        public async Task<dynamic> SettleBet()
        {
            var logName = $"{_tpGameName}_SettleBet";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var response = new SettleResponse
            {
                Results = new List<object>()
            };

            try
            {
                var getReq = JsonConvert.DeserializeObject<SettleRequest_IM>(content);

                var tpId = GetTpId(getReq.ProductWallet);
                var tpGameType = GetTpGameType(getReq.ProductWallet);

                var transactionsList = getReq.Transactions;

                foreach (var settle in transactionsList)
                {
                    var errorResponse = new SettleErrorRespIM
                    {
                        Code = (int)IMResultCode.Exception,
                        Message = "Error",
                        TransactionId = settle.TransactionId
                    };

                    // check tp
                    if (tpId == Tp.None || tpGameType == GameType.None)
                    {
                        errorResponse.Code = (int)IMResultCode.InvalidArgument;
                        errorResponse.Message = "Product Wallet Invalid (TP)";
                        _log2File.Error($"{JsonConvert.SerializeObject(errorResponse)}", logName);
                        return errorResponse;
                    }

                    var tpUserName = settle.PlayerId;

                    var result = _imBll.Settle_IM(settle, tpUserName, tpId, tpGameType, out var outData);
                    if (result == -1)
                    {
                        errorResponse.Code = (int)IMResultCode.InvalidArgument;
                        errorResponse.Message = "Already accepted";
                        _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        response.Results.Add(errorResponse);
                        continue;
                    }
                    if (result == -2)
                    {
                        errorResponse.Code = (int)IMResultCode.TransactionIdNotFound;
                        errorResponse.Message = "TransactionId Not Found";
                        _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        response.Results.Add(errorResponse);
                        continue;
                    }

                    _log2File.Info($"{content}\r\n Response: {JsonConvert.SerializeObject(outData)}", logName);
                    response.Results.Add(outData);
                }

                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
                return response;
            }
        }

        [HttpPost]
        [Route("[controller]/Refund")]
        public async Task<dynamic> Refund()
        {
            var logName = $"{_tpGameName}_Refund";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var response = new SettleResponse
            {
                Results = new List<object>()
            };

            try
            {
                var getReq = JsonConvert.DeserializeObject<RefundRequest_Spribe>(content);

                var transactionsList = getReq.Transactions;

                foreach (var refundData in transactionsList)
                {
                    var errorResponse = new SettleErrorRespIM
                    {
                        Code = (int)IMResultCode.Exception,
                        Message = "Error",
                        TransactionId = refundData.TransactionId
                    };

                    var tpUserName = refundData.PlayerId;

                    if (refundData.TransactionType != "Cancel")
                    {
                        errorResponse.Code = (int)IMResultCode.InvalidArgument;
                        errorResponse.Message = "TransactionType Invalid Argument";
                        _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        response.Results.Add(errorResponse);
                        continue;
                    }

                    var result = _imBll.Refund_SPRIBE(refundData, tpUserName, Tp.SPRIBE, GameType.SPRIBE_RNG, out var outData);
                    if (result == -1)
                    {
                        errorResponse.Code = (int)IMResultCode.InvalidArgument;
                        errorResponse.Message = "Already accepted";
                        _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        response.Results.Add(errorResponse);
                        continue;
                    }
                    if (result == -2)
                    {
                        errorResponse.Code = (int)IMResultCode.TransactionIdNotFound;
                        errorResponse.Message = "TransactionId Not Found";
                        _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(errorResponse)}", logName);
                        response.Results.Add(errorResponse);
                        continue;
                    }

                    _log2File.Info($"{content}\r\n Response: {JsonConvert.SerializeObject(outData)}", logName);
                    response.Results.Add(outData);
                }

                return response;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
                return response;
            }
        }

        private Tp GetTpId(string productWallet)
        {
            if (_productWalletToTpId.ContainsKey(productWallet))
            {
                return _productWalletToTpId[productWallet];
            }
            return Tp.None;
        }

        private GameType GetTpGameType(string productWallet)
        {
            if (_productWalletToTpGameType.ContainsKey(productWallet))
            {
                return _productWalletToTpGameType[productWallet];
            }
            return GameType.None;
        }
    }
}