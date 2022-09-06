using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpMerchantBalance.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly IHostEnvironment env;
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly ILogger<APIController> logger;
        private readonly Log2File log2File;

        public APIController(IHostEnvironment env, IConfiguration configuration, ILogger<APIController> logger, Common common, TpCustInfoService tpCustInfoService, ThirdPartyBLL thirdPartyBLL, Log2File log2File)
        {
            this.env = env;
            ThirdPartyHelperBase.EnvironmentName = env.EnvironmentName;
            ThirdPartyHelperBase.IsDevelopment = env.IsDevelopment();
            ThirdPartyHelperBase.ContentRootPath = env.ContentRootPath;
            ThirdPartyHelperBase.CommonServiceHost = configuration.GetValue<string>("AiLabServiceHost:CommonServiceHost");
            ThirdPartyHelperBase.LetsTalkGroupName = configuration.GetValue<string>("LetsTalkConfig:LetsTalkGroupName");
            this.configuration = configuration;
            this.common = common;
            this.tpCustInfoService = tpCustInfoService;
            this.thirdPartyBLL = thirdPartyBLL;
            this.logger = logger;
            this.log2File = log2File;
        }

        [HttpGet]
        public ActionResult<IEnumerable<string>> ApiInfo()
        {
            return new string[] { env.EnvironmentName };
        }

        [HttpGet]
        public ActionResult<ApiResult<TpCustInfo>> test()
        {
            return tpCustInfoService.GetTpCustInfo(4, 1);
        }

        [HttpPost]
        public ActionResult<ApiResult> GotoGame([FromBody] TpInfo tpInfo)
        {
            var apiResult = new ApiResult();

            try
            {            
                apiResult = thirdPartyBLL.GotoGame(tpInfo);
                logger.LogInformation(apiResult.Message);

                if (apiResult.ErrorCode == ErrorCode.Success && tpInfo.CustId != 0)
                {
                    thirdPartyBLL.AutoTransfer(tpInfo);
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.Transfer(tpTransfer);

                if (apiResult.ErrorCode != 0)
                {
                    log2File.Error(JsonConvert.SerializeObject(apiResult), $"{tpTransfer.TpId}_Transfer");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
                log2File.Error(ex.ToString(), $"{tpTransfer.TpId}_Transfer");
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
                apiResult = thirdPartyBLL.GetBalance(tpInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.Logout(tpInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.BonusTransfer(tpBonusTransfer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.BonusCancelTransfer(tpBonusCancelTransfer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.FixNegative(tpInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.AdjustWallet(adminTpTransfer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                apiResult = thirdPartyBLL.SetBetLimit(setBetLimit);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }
            return apiResult;
        }

        public ActionResult<ApiResult> GetBetLimit(GetBetLimit getBetLimit)
        {
            var apiResult = new ApiResult();
            try
            {
                apiResult = thirdPartyBLL.GetBetLimit(getBetLimit);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
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
                return thirdPartyBLL.GetBetDetail(request);
            }
            catch (BetDetailNotFoundException ex)
            {
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.BetDetailNotFound,
                    RawMessage = ex.Message,
                };
            }
            catch (NotImplementedException ex)
            {
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.GetBetDetailNotImplemented,
                    RawMessage = ex.Message,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.SystemError,
                    RawMessage = ex.Message,
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
                return thirdPartyBLL.GetMerchantBalance(tpMerchantInfo);
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
                logger.LogError(ex, ex.Message);
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
                return thirdPartyBLL.GetTpBetRecordExtend(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new ApiResult<TpBetRecordExtend>()
                {
                    ErrorCode = ErrorCode.SystemError,
                    RawMessage = ex.Message,
                };
            }
        }
    }
}
