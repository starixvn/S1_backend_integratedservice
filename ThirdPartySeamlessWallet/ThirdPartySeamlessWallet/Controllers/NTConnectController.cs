using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class NTConnectController : ControllerBase
    {
        private readonly INTConnectBLL _NTConnectBLL;
        private readonly Tp _thisTp;
        private readonly Log2File _log2File;

        public NTConnectController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _NTConnectBLL = provider.GetRequiredService<INTConnectBLL>();
            _thisTp = Tp.NTConnect; 
        }

        [HttpPost]
        [Route("[controller]/Player/GetBalance")]
        public string GetBalance(NTCGetBalanceRequest request)
        {
            
            var logName = $"{_thisTp}_GetBalance";
            var baseResult = BaseResult();
            
            try
            {
                var getBalance = _NTConnectBLL.GetBalance(request);
                return JsonConvert.SerializeObject(getBalance);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return JsonConvert.SerializeObject(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/Bet/CheckBet")]
        public string CheckBet(NTCCheckBetRequest request)
        {
            var logName = $"{_thisTp}_CheckBet";
            var baseResult = BaseResult();
            try
            {
                var placeBetResult = _NTConnectBLL.CheckBet(request);
                return JsonConvert.SerializeObject(placeBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            
            return JsonConvert.SerializeObject(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/Bet/PlaceBet")]
        public string PlaceBet(NTCPlaceBetRequest request)
        {
            var logName = $"{_thisTp}_PlaceBet";
            var baseResult = BaseResult();
            try
            {
                var placeBetResult = _NTConnectBLL.PlaceBet(request);
                return JsonConvert.SerializeObject(placeBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            
            return JsonConvert.SerializeObject(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/Bet/SettleBet")]
        public string SettleBet(NTCSettleBetRequest request)
        {
            var logName = $"{_thisTp}_SettleBet";
            var baseResult = BaseResult();
            
            try
            {
                var settleBetResult = _NTConnectBLL.SettleBet(request);
                return JsonConvert.SerializeObject(settleBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return JsonConvert.SerializeObject(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/Bet/CancelBet")]
        public string CancelBet(NTCCancelBetRequest request)
        {
            var logName = $"{_thisTp}_CancelBet";
            var baseResult = BaseResult();
            
            try
            {
                var settleBetResult = _NTConnectBLL.CancelBet(request);
                return JsonConvert.SerializeObject(settleBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return JsonConvert.SerializeObject(baseResult);
        }
        
        #region private

        private NTCResponse<NTCErrorResponse> BaseResult()
        {
            return new NTCResponse<NTCErrorResponse>()
            {
                status = "error",
                data = new NTCGetBalanceResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };
        }
        
        #endregion
    }
}