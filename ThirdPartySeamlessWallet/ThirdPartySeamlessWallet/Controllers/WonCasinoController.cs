using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class WonCasinoController : ControllerBase
    {
        private readonly IWonCasinoBLL _IWonCasinoBLL;
        private readonly Tp _thisTp;
        private readonly Log2File _log2File;

        public WonCasinoController(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _IWonCasinoBLL = provider.GetRequiredService<IWonCasinoBLL>();
            _thisTp = Tp.WonCasino; 
        }

        [HttpPost]
        [Route("[controller]/api/balance")]
        public IActionResult GetBalance(WonCasinoBaseRequest request)
        {
            
            var logName = $"{_thisTp}_GetBalance";
            var baseResult = BaseResult();
            
            try
            {
                var getBalance = _IWonCasinoBLL.GetBalance(request);
                return Ok(getBalance);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return Ok(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/api/deduct")]
        public IActionResult Deduct(WonCasinoDeductRequest request)
        {
            var logName = $"{_thisTp}_Deduct";
            var baseResult = BaseResult();
            try
            {
                var placeBetResult = _IWonCasinoBLL.Deduct(request);
                return Ok(placeBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            
            return Ok(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/api/deposit")]
        public IActionResult Deposit(WonCasinoDepositRequest request)
        {
            //Deposit = Settle
            var logName = $"{_thisTp}_Deposit";
            var baseResult = BaseResult();
            
            try
            {
                var settleBetResult = _IWonCasinoBLL.Deposit(request);
                return Ok(settleBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return Ok(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/api/rollback")]
        public IActionResult RollBack(WonCasinoRollBackRequest request)
        {
            var logName = $"{_thisTp}_RollBack";
            var baseResult = BaseResult();
            
            try
            {
                var settleBetResult = _IWonCasinoBLL.RollBack(request);
                return Ok(settleBetResult);
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }
            return Ok(baseResult);
        }
        
        #region private

        private WonCasinoBaseResponse BaseResult()
        {
            return new WonCasinoBaseResponse()
            {
                result = (int)WonCasinoErrorCodeEnum.SystemError,
                desc = WonCasinoErrorCodeEnum.SystemError.GetEnumDescription()
            };
        }
        
        #endregion
    }
}