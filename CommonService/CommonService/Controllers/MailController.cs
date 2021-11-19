using System;
using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly SendXBLL sendXBLL;
        private readonly SendGridBLL sendGridBLL;
        private readonly RestPasswordBLL restPasswordBLL;
        private readonly OTPEmailBLL _otpEmailBll;
        private readonly ILogger<MailController> logger;

        public MailController(ILogger<MailController> logger, CustInfoBLL custInfoBLL, SendXBLL sendXBLL, SendGridBLL sendGridBLL, RestPasswordBLL restPasswordBLL, OTPEmailBLL otpEmailBll)
        {
            this.custInfoBLL = custInfoBLL;
            this.sendXBLL = sendXBLL;
            this.sendGridBLL = sendGridBLL;
            this.restPasswordBLL = restPasswordBLL;
            _otpEmailBll = otpEmailBll;
            this.logger = logger;
        }
        public class Req
        {
            public int custId { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SendWelcomeMailAsync(Req req)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null)
            {
                var resultResp = await sendXBLL.IdentifyAsync(custInfo);

                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;

                //if (apiResult.ErrorCode == 0)
                //{
                //    resultResp = await sendXBLL.WelcomeMailAsync(custInfo);
                //    apiResult.ErrorCode = resultResp.ErrorCode;
                //    apiResult.Message = resultResp.Message;
                //    apiResult.RawMessage = resultResp.RawMessage;
                //}
            }

            return apiResult;
        }

        public class Req1
        {
            public int custId { get; set; }
            public string subject { get; set; }
            public string content { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SendMailAsync(Req1 req)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null)
            {
                var resultResp = await sendGridBLL.SendMailAsync(custInfo, req.subject, req.content);

                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;

                //if (apiResult.ErrorCode == 0)
                //{
                //    resultResp = await sendXBLL.WelcomeMailAsync(custInfo);
                //    apiResult.ErrorCode = resultResp.ErrorCode;
                //    apiResult.Message = resultResp.Message;
                //    apiResult.RawMessage = resultResp.RawMessage;
                //}
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
            logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(req2));
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(req2.custId);
            if (custInfo != null)
            {
                var resultResp = await restPasswordBLL.SendMailAsync(custInfo, req2.resetPasswordUrl);
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