using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
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
        private readonly ILogger<IMController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly IIMBLL _imBll;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly string _tpGameName;

        public IMController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<IMController>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _imBll = provider.GetRequiredService<IIMBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = Tp.IM;
            _tpGameName = "IM";
        }

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
        public async Task<GetBalanceResp> GetBalance(GetBalanceReq req)
        {
            var logName = $"{_tpGameName}_GetBalance";
            _log2File.Info($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(req)}", logName);
            var checkPlayerResp = new GetBalanceResp();

            try
            {
                // check user (exist status online)
                var tpUserName = GetTpUserName(req.PlayerId);
                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, tpUserName);
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
                else
                {
                    checkPlayerResp.PlayerId = req.PlayerId ;
                    checkPlayerResp.Currency = req.Currency;
                    checkPlayerResp.Message = "Successful";
                    checkPlayerResp.Code = (int)IMResultCode.Success;
                }

                var gameType = req.ProductWallet switch
                {
                    "IMSportsbook" => (int) GameType.IM_Sport,
                    "IMESports" => (int) GameType.IM_Esport,
                    "IMSlot" => (int) GameType.IM_Spribe ,
                    _ => 0
                };
            
                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)_thisTp, tpUserName, gameType);
            
                checkPlayerResp.Balance = tpCustBalance.Balance;

                _log2File.Info($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }
            catch (Exception e)
            {
                checkPlayerResp.Code = (int)IMResultCode.Exception;
                checkPlayerResp.Message = "Unknown Error";
                _log2File.Error($"account={req.PlayerId}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName ,e);
                return checkPlayerResp;
            }
        }

        [HttpPost]
        [Route("[controller]/PlaceBet")]
        public async Task<dynamic> PlaceBet()
        {
            var logName = $"{_thisTp}_bet";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            
            var getErrorResponse = new ErrorRespIM
            {
                Code = (int)IMResultCode.Exception,
                Message = "Error"
            };
            
            try
            {
                var getReq = JsonConvert.DeserializeObject<BettingData_IM>(content);
                var gameType = getReq.ProductWallet switch
                {
                    "IMSportsbook" =>  GameType.IM_Sport,
                    "IMESports" =>  GameType.IM_Esport,
                    "IMSlot" =>  GameType.IM_Spribe ,
                    _ => GameType.IM_Sport
                };
                
                var transactionsList = getReq.Transactions;
                
                var tpUserName = GetTpUserName(transactionsList.FirstOrDefault().PlayerId);
                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)_thisTp, tpUserName);
                if (tpCheck == null)
                {
                    getErrorResponse.Code = (int)IMResultCode.PlayerDoesNotExist;
                    getErrorResponse.Message = "Player Does Not Exist";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                
                if (tpCheck.Locked)
                {
                    getErrorResponse.Code = (int)IMResultCode.PlayerIsInactive;
                    getErrorResponse.Message = "Player Is Inactive";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                
                var custBalance = _tpCustInfoBll.GetBalance((int)Tp.IM, tpUserName, (int)gameType);
                var betSum = transactionsList.Sum(x => x.Amount);
                var checkBalance = custBalance.Balance - betSum;
                if (checkBalance < 0)
                {
                    getErrorResponse.Code = (int)IMResultCode.NotEnoughBalance;
                    getErrorResponse.Message = "Not enough balance";
                    _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }
                
                var result = _imBll.Bet_IM(transactionsList, tpUserName, gameType , out var outData);

                if (result > 0)
                {
                    _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(outData)}", logName);
                    return outData;
                }

                if( result == -1)
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
        
        private string GetTpUserName(string tpUserName)
        {
            tpUserName = tpUserName.Replace("IM22E_" , "");
            return tpUserName;
        }
    }
}