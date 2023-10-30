using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class FPSController : ControllerBase
    {
        private readonly CustInfoBLL _custInfoBLL;
        private readonly FPSBLL _fpsBLL;

        public FPSController(IServiceProvider provider)
        {
            _fpsBLL = provider.GetRequiredService<FPSBLL>();
            _custInfoBLL = provider.GetRequiredService<CustInfoBLL>();
        }

        [HttpPost]
        public async Task<ApiResult> FPSChecksAsync(FPSChecksRequest fpsChecksRequest)
        {
            var apiResult = new ApiResult();
            var custInfo = _custInfoBLL.GetCustInfoProfile(fpsChecksRequest.account);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.TpUserName))
            {
                var resultResp = await _fpsBLL.FPSChecksAsync(custInfo, fpsChecksRequest);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }

            return apiResult;
        }
    }
}
