using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Model.TpMerchantBalance.Response;

using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly TpCustInfoService _tpCustInfoService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<APIController> _logger;

        public APIController(IServiceProvider provider)
        {
            _env = provider.GetRequiredService<IWebHostEnvironment>();
            _tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<APIController>>();

            ThirdPartyHelperBase.EnvironmentName = _env.EnvironmentName;
            ThirdPartyHelperBase.IsDevelopment = _env.IsDevelopment();
            ThirdPartyHelperBase.ContentRootPath = _env.ContentRootPath;
            ThirdPartyHelperBase.SeamlessServiceHost = provider.GetRequiredService<ConfigBLL>()
                .GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").SeamlessService;
            ThirdPartyHelperBase.logger = provider.GetRequiredService<ILogger<IThirdPartyHelper>>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> ApiInfo()
        {
            return new string[] { _env.EnvironmentName };
        }

        [HttpGet]
        public ActionResult<ApiResult<TpCustInfo>> test()
        {
            return _tpCustInfoService.GetTpCustInfo(4, 1);
        }

        [HttpPost]
        public ActionResult<ApiResult> GotoGame([FromBody] TpInfo tpInfo)
        {
            var apiResult = new ApiResult();

            try
            {
                apiResult = _thirdPartyBLL.GotoGame(tpInfo);
                _logger.LogInformation(apiResult.Message);

                if (apiResult.ErrorCode == ErrorCode.Success && tpInfo.CustId != 0)
                {
                    _thirdPartyBLL.AutoTransfer(tpInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult<TransferResult>> Transfer([FromBody] TpTransfer tpTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            apiResult.ErrorCode = ErrorCode.SystemError;
            apiResult.Data = new TransferResult();

            try
            {
                apiResult = _thirdPartyBLL.Transfer(tpTransfer);

                if (apiResult.ErrorCode != 0)
                {
                    _logger.LogError(JsonConvert.SerializeObject(apiResult));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult<UserBalance>> GetBalance([FromBody] TpInfo tpInfo)
        {
            var apiResult = new ApiResult<UserBalance>();
            apiResult.ErrorCode = ErrorCode.SystemError;

            try
            {
                apiResult = _thirdPartyBLL.GetBalance(tpInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                return apiResult;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult> Logout([FromBody] TpInfo tpInfo)
        {
            var apiResult = new ApiResult();
            apiResult.ErrorCode = ErrorCode.SystemError;

            try
            {
                apiResult = _thirdPartyBLL.Logout(tpInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                return apiResult;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult> BonusTransfer(TpBonusTransfer tpBonusTransfer)
        {
            var apiResult = new ApiResult();
            apiResult.ErrorCode = ErrorCode.SystemError;

            try
            {
                apiResult = _thirdPartyBLL.BonusTransfer(tpBonusTransfer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                return apiResult;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult> BonusCancelTransfer(TpBonusCancelTransfer tpBonusCancelTransfer)
        {
            var apiResult = new ApiResult
            {
                ErrorCode = ErrorCode.SystemError
            };

            try
            {
                apiResult = _thirdPartyBLL.BonusCancelTransfer(tpBonusCancelTransfer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                return apiResult;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult> FixNegative([FromBody] TpInfo tpInfo)
        {
            var apiResult = new ApiResult();
            apiResult.ErrorCode = ErrorCode.SystemError;

            try
            {
                apiResult = _thirdPartyBLL.FixNegative(tpInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                return apiResult;
            }

            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult<AdjustWalletResult>> AdjustWallet([FromBody] AdminTpTransfer adminTpTransfer)
        {
            var apiResult = new ApiResult<AdjustWalletResult>();
            apiResult.ErrorCode = ErrorCode.SystemError;
            apiResult.Data = new AdjustWalletResult();

            try
            {
                apiResult = _thirdPartyBLL.AdjustWallet(adminTpTransfer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        public ActionResult<ApiResult> SetBetLimit(SetBetLimit setBetLimit)
        {
            var apiResult = new ApiResult();
            try
            {
                apiResult = _thirdPartyBLL.SetBetLimit(setBetLimit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }
            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult<object>> GetBetDetail([FromBody] GetBetDetailRequest request)
        {
            try
            {
                return _thirdPartyBLL.GetBetDetail(request);
            }
            catch (BetDetailNotFoundException ex)
            {
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.BetDetailNotFound,
                    Message = ex.Message,
                };
            }
            catch (NotImplementedException ex)
            {
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.GetBetDetailNotImplemented,
                    Message = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.SystemError,
                    Message = ex.Message,
                };
            }
        }

        /// <summary>
        /// 取得商戶餘額
        /// </summary>
        /// <param name="tpMerchantInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ApiResult<MerchantBalanceResponse>> GetMerchantBalance(TpMerchantInfo tpMerchantInfo)
        {
            try
            {
                return _thirdPartyBLL.GetMerchantBalance(tpMerchantInfo);
            }
            catch (GetMerchantBalanceNotImplementedException ex)
            {
                return new ApiResult<MerchantBalanceResponse>()
                {
                    ErrorCode = ErrorCode.GetMerchantBalanceNotImplemented,
                    RawMessage = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<MerchantBalanceResponse>()
                {
                    ErrorCode = ErrorCode.SystemError,
                    RawMessage = ex.Message,
                };
            }
        }

        [HttpPost]
        public ActionResult<ApiResult<TpBetRecordExtend>> GetTpBetRecordExtend([FromBody] GetTpBetRecordExtendRequest request)
        {
            try
            {
                return _thirdPartyBLL.GetTpBetRecordExtend(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ApiResult<TpBetRecordExtend>()
                {
                    ErrorCode = ErrorCode.SystemError,
                    RawMessage = ex.Message,
                };
            }
        }

        [HttpPost]
        public ActionResult<ApiResult> AddPrizeToPlayer([FromBody] AddFreeSpin addFreeSpin)
        {
            var apiResult = new ApiResult();
            try
            {
                apiResult = _thirdPartyBLL.AddPrizeToPlayer(addFreeSpin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }
            return apiResult;
        }

        [HttpPost]
        public ActionResult<ApiResult> CreateBonusCode([FromBody] CreateBonusCode createBonusCode)
        {
            var apiResult = new ApiResult();
            try
            {
                apiResult = _thirdPartyBLL.CreateBonusCode(createBonusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }
            return apiResult;
        }
    }
}