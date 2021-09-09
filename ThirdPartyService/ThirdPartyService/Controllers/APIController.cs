using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
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

                if (apiResult.ErrorCode == ErrorCode.Success)
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
        public ActionResult<ApiResult<TransferResult>> AdjustWallet([FromBody] AdminTpTransfer adminTpTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            apiResult.ErrorCode = ErrorCode.SystemError;
            apiResult.Data = new TransferResult();

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
    }
}