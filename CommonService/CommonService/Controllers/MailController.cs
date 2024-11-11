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
        private readonly RestPasswordBLL _restPasswordBll;
        private readonly OTPEmailBLL _otpEmailBll;
        private readonly Dictionary<int, int> _welComeMailConfig;
        private readonly IBMMailBLL _iBMMailBll;

        public MailController(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _restPasswordBll = provider.GetRequiredService<RestPasswordBLL>();
            _otpEmailBll = provider.GetRequiredService<OTPEmailBLL>();
            _iBMMailBll = provider.GetRequiredService<IBMMailBLL>();
            _welComeMailConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<int>("WelComeMail");
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
            
            var config = _welComeMailConfig[custInfo.SiteId];
            if (config == 1)
            {
                //return await _sendGridBll.SendMailAsync(custInfo, null, TemplateType.WelcomeMail);
                return await _iBMMailBll.SendMailAsync(custInfo, null);
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Success;
                return apiResult;
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
                return await _restPasswordBll.SendMailAsync(custInfo, req2.resetPasswordUrl);
            }
            return apiResult;
        }

        [HttpPost]
        public async Task<ApiResult<SendOTPEmailResult>> OTPEmail(OTPEmailReq req)
        {
            return await _otpEmailBll.SendOtpEmailAsync(req.custId, req.OTPEmailUrl);
        }

        [HttpPost]
        public ApiResult VerifyOTP(VerifyOTPEmailReq req)
        {
            return _otpEmailBll.VerifyOtp(req.CustId, req.InputOTP);
        }
    }
}