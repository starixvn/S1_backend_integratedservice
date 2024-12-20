using Microsoft.AspNetCore.Mvc;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.BLL;

namespace ThirdPartyService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ExtendController : ControllerBase
    {
        private readonly ILogger<ExtendController> _logger;
        private readonly ThirdPartyExtendBLL _thirdPartyExtendBLL;

        public ExtendController(IServiceProvider provider)
        {
            _thirdPartyExtendBLL = provider.GetRequiredService<ThirdPartyExtendBLL>();
            _logger = provider.GetRequiredService<ILogger<ExtendController>>();
        }

        #region SBSW app use

        [HttpPost]
        public ActionResult<ApiResult<GetTokenResult>> GetToken(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<GetTokenResult>();

            try
            {
                apiResult = _thirdPartyExtendBLL.GetToken(tpInfo.CustId, tpInfo.TpId);
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
        public ActionResult<ApiResult> InvalidateToken(TpInfo tpInfo)
        {
            var apiResult = new ApiResult();

            try
            {
                apiResult = _thirdPartyExtendBLL.InvalidateToken(tpInfo.CustId, tpInfo.TpId);
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
        public ActionResult<ApiResult<VerifyTokenResult>> VerifyToken(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<VerifyTokenResult>();

            try
            {
                if (!tpInfo.inputs.TryGetValue("Token", out var token))
                {
                    _logger.LogError("token not found.");
                    apiResult.ErrorCode = ErrorCode.SystemError;
                    apiResult.Message = "request token not found.";
                    return apiResult;
                }
                apiResult = _thirdPartyExtendBLL.VerifyToken(tpInfo.SiteId, tpInfo.TpId, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                apiResult.ErrorCode = ErrorCode.SystemError;
                apiResult.RawMessage = ex.Message;
            }

            return apiResult;
        }

        #endregion
    }
}
