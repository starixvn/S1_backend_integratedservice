using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
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
        private readonly Dictionary<int, NTConnectConfig> _siteConfigDic;
        private readonly Log2File _log2File;

        public NTConnectController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _NTConnectBLL = provider.GetRequiredService<INTConnectBLL>();
            _thisTp = Tp.NTConnect; 
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NTConnectConfig>(Tp.NTConnect.ToString());
        }

        [HttpPost]
        [Route("[controller]/Player/GetBalance")]
        public string GetBalance(NTCGetBalanceRequest request)
        {
            
            var logName = $"{_thisTp}_authenticate";
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
            var logName = $"{_thisTp}_checkBet";
            var baseResult = BaseResult();
            try
            {
                var checkBet = new NTCCheckBetResponse()
                {
                    stake = 100,
                    win_amt = 195,
                    status = "A",
                    bet_date = DateTime.Now
                };

                var result = new NTCResponse<NTCCheckBetResponse>()
                {
                    status = "success",
                    data = checkBet
                };
                return JsonConvert.SerializeObject(result);
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