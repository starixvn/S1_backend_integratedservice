using CommonService.Service.BLL;
using CommonService.Service.DAL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;
using System.Configuration;

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
        private readonly Dictionary<int, int> _welComeMailConfig;
        private readonly MailSMTPBLL _smtpMailBll;
        private readonly IConfiguration _configuration;

        public MailController(IServiceProvider provider, IConfiguration configuration)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();
            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _restPasswordBll = provider.GetRequiredService<RestPasswordBLL>();
            _otpEmailBll = provider.GetRequiredService<OTPEmailBLL>();
            _welComeMailConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<int>("WelComeMail");
            _smtpMailBll = provider.GetRequiredService<MailSMTPBLL>();
            _configuration = configuration;
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
                var resultResp = await _sendGridBll.SendMailAsync(custInfo, null, TemplateType.WelcomeMail);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;
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
            var isUsingMailSMTP = _configuration.GetValue<bool>("IsUsingMailSMTP");
            var apiResult = new ApiResult();
            var custInfo = _custInfoBll.GetCustInfoProfile(req2.custId);
            if (custInfo != null)
            {
                var resultResp = new ApiResult();
                if (isUsingMailSMTP)
                {
                    resultResp = await _smtpMailBll.SendMailAsync(custInfo, req2.resetPasswordUrl, TemplateType.ResetPassword);
                }
                else
                {
                    resultResp = await _restPasswordBll.SendMailAsync(custInfo, req2.resetPasswordUrl);
                }
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