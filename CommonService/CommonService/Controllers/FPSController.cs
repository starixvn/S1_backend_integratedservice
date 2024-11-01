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
        private readonly CustInfoBLL _custInfoBll;
        private readonly FPSBLL _fpsBll;

        public FPSController(IServiceProvider provider)
        {
            _fpsBll = provider.GetRequiredService<FPSBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
        }

        [HttpPost]
        public async Task<ApiResult> FPSChecksAsync(FPSChecksRequest fpsChecksRequest)
        {
            var apiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = string.Empty
            };
            //Todo
            //var custInfo = _custInfoBll.GetCustInfoProfile(fpsChecksRequest.Account);

            //if (custInfo != null && !string.IsNullOrEmpty(custInfo.TpUserName))
            //{
            //    var resultResp = await _fpsBll.FPSChecksAsync(custInfo, fpsChecksRequest);
            //    apiResult.ErrorCode = resultResp.ErrorCode;
            //    apiResult.Message = resultResp.Message;
            //}
            //else
            //{
            //    apiResult.ErrorCode = ErrorCode.UserNotExist;
            //}

            return apiResult;
        }
    }
}
