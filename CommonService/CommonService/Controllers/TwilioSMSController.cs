using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class TwilioSMSController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly TwilioSMSBLL twilioSMSBLL;
        public TwilioSMSController(TwilioSMSBLL twilioSMSBLL, CustInfoBLL custInfoBLL)
        {
            this.twilioSMSBLL = twilioSMSBLL;
            this.custInfoBLL = custInfoBLL;
        }
        [HttpPost]
        public async Task<ApiResult> VerificationsSMSAsync(TwilioVerificationsSMSReq twilioVerificationsSMSReq)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(twilioVerificationsSMSReq.custId);

            if (custInfo != null && !string.IsNullOrEmpty(custInfo.PhoneNo) && !custInfo.PhoneVerified)
            {
                var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
                var phone = $"{custInfo.AreaCode}{phoneNo}";
                var resultResp = await twilioSMSBLL.SendVerifySMSAsync(custInfo, phone, twilioVerificationsSMSReq.ip);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawErrorCode = resultResp.RawErrorCode;
                apiResult.RawMessage = resultResp.RawMessage;
            }
            else if (custInfo != null && string.IsNullOrEmpty(custInfo.PhoneNo))
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
            }
            else if (custInfo.PhoneVerified)
            {
                apiResult.ErrorCode = ErrorCode.Verified;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }

            return apiResult;
        }
        [HttpPost]
        public async Task<ApiResult> VerificationCheckSMSAsync(TwilioVerificationCheckSMSReq twilioVerificationCheckSMSReq)
        {

            ApiResult apiResult = new ApiResult();
            var resultResp = await twilioSMSBLL.VerificationCheckSMSAsync(twilioVerificationCheckSMSReq.siteName, twilioVerificationCheckSMSReq.sid, twilioVerificationCheckSMSReq.code);
            apiResult.ErrorCode = resultResp.ErrorCode;
            apiResult.Message = resultResp.Message;
            apiResult.RawErrorCode = resultResp.RawErrorCode;
            apiResult.RawMessage = resultResp.RawMessage;

            return apiResult;
        }
    }
}
