using CommonService.Service.BLL;
using CommonService.Service.DAL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly CustInfoBLL _custInfoBll;
        private readonly SendGridBLL _sendGridBll;
        private readonly RestPasswordBLL _restPasswordBll;
        private readonly OTPEmailBLL _otpEmailBll;

        public MailController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _restPasswordBll = provider.GetRequiredService<RestPasswordBLL>();
            _otpEmailBll = provider.GetRequiredService<OTPEmailBLL>();
        }

        public class Req
        {
            public int custId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SendWelcomeMailAsync(Req req)
        {
            var apiResult = new ApiResult();
            var custInfo = _custInfoBll.GetCustInfoProfile(req.custId);

            if (custInfo != null)
            {
                var resultResp = await _sendGridBll.SendMailAsync(custInfo, null, TemplateType.WelcomeMail);

                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
            }

            return apiResult;
        }

        public class Req2
        {
            public int custId { get; set; }
            public string resetPasswordUrl { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> RestPasswordAsync(Req2 req2)
        {
            var apiResult = new ApiResult();
            var custInfo = _custInfoBll.GetCustInfoProfile(req2.custId);
            if (custInfo != null)
            {
                var resultResp = await _restPasswordBll.SendMailAsync(custInfo, req2.resetPasswordUrl);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
            }
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPEmailResult>> OTPEmail(OTPEmailReq req)
        {
            var apiResult = await _otpEmailBll.SendOtpEmailAsync(req.custId, req.OTPEmailUrl);
            return apiResult;
        }

        [HttpPost]
        public ApiResult VerifyOTP(VerifyOTPEmailReq req)
        {
            return _otpEmailBll.VerifyOtp(req.CustId, req.InputOTP);
        }
    }
}