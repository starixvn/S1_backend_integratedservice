using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class MoviderSMSController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly MoviderSMSBLL moviderSMSBLL;
        public MoviderSMSController(MoviderSMSBLL moviderSMSBLL, CustInfoBLL custInfoBLL)
        {
            this.moviderSMSBLL = moviderSMSBLL;
            this.custInfoBLL = custInfoBLL;
        }
        [HttpPost]
        public async Task<ApiResult> SendVerifySMSAsync(SendVerifySMSReq sendVerifySMSReq)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(sendVerifySMSReq.custId);
            if (custInfo != null)
            {
                var resultResp = await moviderSMSBLL.SendVerifySMSAsync(custInfo, sendVerifySMSReq.phone);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
            }
            return apiResult;
        }
        [HttpPost]
        public async Task<ApiResult> AcknowledgeVerifySMSAsync(AcknowledgeVerifySMSReq acknowledgeVerifySMSReq)
        {

            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(acknowledgeVerifySMSReq.custId);
            if (custInfo != null)
            {
                var resultResp = await moviderSMSBLL.AcknowledgeVerifySMSAsync(custInfo, acknowledgeVerifySMSReq.request_id, acknowledgeVerifySMSReq.code);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
            }
            return apiResult;
        }


    }
}
