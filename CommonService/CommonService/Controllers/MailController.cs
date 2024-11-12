using CommonService.Service.BLL;
using CommonService.Service.Model;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly WelcomeEmailBLL _welcomeEmailBll;
        private readonly RestPasswordBLL _restPasswordBll;
        private readonly OTPEmailBLL _otpEmailBll;

        public MailController(IServiceProvider provider)
        {
            _welcomeEmailBll = provider.GetRequiredService<WelcomeEmailBLL>();
            _restPasswordBll = provider.GetRequiredService<RestPasswordBLL>();
            _otpEmailBll = provider.GetRequiredService<OTPEmailBLL>();
        }

        public class SendWelcomeMailRequest
        {
            public int custId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SendWelcomeMailAsync(SendWelcomeMailRequest request)
        {
            return await _welcomeEmailBll.SendMailAsync(request.custId);
        }

        public class ResetPasseordRequest
        {
            public int custId { get; set; }
            public string resetPasswordUrl { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> RestPasswordAsync(ResetPasseordRequest request)
        {
            return await _restPasswordBll.SendMailAsync(request.custId, request.resetPasswordUrl);
        }

        /// <summary>
        /// 發送OTP To Mail
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult<SendOTPEmailResult>> OTPEmail(OTPEmailReq req)
        {
            return await _otpEmailBll.SendOtpEmailAsync(req.custId, req.OTPEmailUrl);
        }

        /// <summary>
        /// Mail點擊btn驗證OTP
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public ApiResult VerifyOTP(VerifyOTPEmailReq req)
        {
            return _otpEmailBll.VerifyOtp(req.CustId, req.InputOTP);
        }
    }
}