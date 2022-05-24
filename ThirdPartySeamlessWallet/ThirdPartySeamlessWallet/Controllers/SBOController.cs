using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/api")]
    public class SBOController : ControllerBase
    {
        private readonly ISBOBLL _sbobll;
        private readonly ILogger<SBOController> _logger;
        public SBOController(ISBOBLL sbobll, ILogger<SBOController> logger)
        {
            _sbobll = sbobll;
            _logger = logger;
        }

        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("GetBalance")]
        public SBOBalanceResponse GetBalance(SBOBalanceRequest request)
        {
            try
            {
                return _sbobll.GetBalance(request);
            }
            catch (SBOException ex)
            {
                _logger.LogError(ex.ErrorMessage);
                return new SBOBalanceResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBOBalanceResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Deduct")]
        public async Task<SBODeductResponse> Deduct(SBODeductRequest request)
        {
            try
            {
                return await _sbobll.Deduct(request);
            }
            catch (SBOException ex)
            {
                return new SBODeductResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBODeductResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Settle")]
        public async Task<SBOSettleResponse> Settle(SBOSettleRequest request)
        {
            try
            {
                return await _sbobll.Settle(request);
            }
            catch (SBOException ex)
            {
                return new SBOSettleResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBOSettleResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("GetBetStatus")]
        public async Task<SBOGetBetStatusResponse> GetBetStatus(SBOGetBetStatusRequest request)
        {
            try
            {
                return await _sbobll.GetBetStatus(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBOGetBetStatusResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Rollback")]
        public SBOBalanceResponse Rollback()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Cancel")]
        public SBOBalanceResponse Cancel()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Tip")]
        public SBOBalanceResponse Tip()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("Bonus")]
        public SBOBalanceResponse Bonus()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("ReturnStake")]
        public SBOBalanceResponse ReturnStake()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
        
        /// <summary>
        /// 下注
        /// </summary>
        [HttpPost]
        [Route("LiveCoinTransaction")]
        public SBOBalanceResponse LiveCoinTransaction()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
    }
}