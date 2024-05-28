using Microsoft.AspNetCore.Mvc;
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
        /// 结算投注
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
            catch (SBOException ex)
            {
                return new SBOGetBetStatusResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
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
        /// 回滚
        /// </summary>
        [HttpPost]
        [Route("Rollback")]
        public async Task<SBORollbackResponse> Rollback(SBORollbackRequest request)
        {
            try
            {
                return await _sbobll.Rollback(request);
            }
            catch (SBOException ex)
            {
                return new SBORollbackResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBORollbackResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }

        /// <summary>
        /// 取消投注
        /// </summary>
        [HttpPost]
        [Route("Cancel")]
        public async Task<SBOCancelResponse> Cancel(SBOCancelRequest request)
        {
            try
            {
                return await _sbobll.Cancel(request);
            }
            catch (SBOException ex)
            {
                return new SBOCancelResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBOCancelResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }

        /// <summary>
        /// 小费
        /// </summary>
        [HttpPost]
        [Route("Tip")]
        public SBOBalanceResponse Tip()
        {
            return new SBOBalanceResponse();
        }

        /// <summary>
        /// 红利
        /// </summary>
        [HttpPost]
        [Route("Bonus")]
        public SBOBalanceResponse Bonus()
        {
            return new SBOBalanceResponse();
        }

        /// <summary>
        /// 归还注额
        /// </summary>
        [HttpPost]
        [Route("ReturnStake")]
        public SBOBalanceResponse ReturnStake()
        {
            return new SBOBalanceResponse();
        }

        /// <summary>
        /// LiveCoin购买
        /// </summary>
        [HttpPost]
        [Route("LiveCoinTransaction")]
        public async Task<SBOLiveCoinTransactionResponse> LiveCoinTransaction(SBOLiveCoinTransactionRequest request)
        {
            try
            {
                return await _sbobll.LiveCoinTransaction(request);
            }
            catch (SBOException ex)
            {
                return new SBOLiveCoinTransactionResponse()
                {
                    ErrorCode = (int)ex.SBOErrorCode,
                    ErrorMessage = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new SBOLiveCoinTransactionResponse()
                {
                    ErrorCode = (int)SBOErrorCode.InternalError,
                    ErrorMessage = SBOErrorCode.InternalError.GetEnumDescription()
                };
            }
        }
    }
}