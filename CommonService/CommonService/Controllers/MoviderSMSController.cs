using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
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

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var phone = $"{custInfo.AreaCode}{phoneNo}";
                var resultResp = await moviderSMSBLL.SendVerifySMSAsync(custInfo, phone);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
            }
            else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
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
