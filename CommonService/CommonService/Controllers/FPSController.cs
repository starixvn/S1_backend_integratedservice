using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class FPSController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly FPSBLL fpsBLL;
        public FPSController(FPSBLL fpsBLL, CustInfoBLL custInfoBLL)
        {
            this.fpsBLL = fpsBLL;
            this.custInfoBLL = custInfoBLL;
        }
        [HttpPost]
        public async Task<ApiResult> FPSChecksAsync(FPSChecksRequest fpsChecksRequest)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(fpsChecksRequest.account);

            if (custInfo != null)
            {
                var resultResp = await fpsBLL.FPSChecksAsync(custInfo, fpsChecksRequest);
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
