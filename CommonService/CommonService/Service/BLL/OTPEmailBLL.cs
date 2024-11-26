using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;

namespace CommonService.Service.BLL
{
    public class OTPEmailBLL
    {
        private readonly SendGridBLL _sendGridBll;
        private readonly MailSMTPBLL _smtpMailBll;
        private readonly OTPSMSService _otpService;
        private readonly CustInfoService _custInfoService;
        private readonly Tools _tools;
        private readonly AuthBLL _authBLL;
        private readonly IConfiguration _configuration;

        public OTPEmailBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _smtpMailBll = provider.GetRequiredService<MailSMTPBLL>();
            _otpService = provider.GetRequiredService<OTPSMSService>();
            _tools = provider.GetRequiredService<Tools>();
            _custInfoService = provider.GetRequiredService<CustInfoService>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _configuration = configuration;
        }

        public async Task<ApiResult<SendOTPEmailResult>> SendOtpEmailAsync(int custId, string url)
        {
            var isUsingMailSMTP = _configuration.GetValue<bool>("IsUsingMailSMTP");

            var apiResult = new ApiResult<SendOTPEmailResult>();
            var custInfo = _custInfoService.GetCustInfoProfile(custId);
            if (custInfo != null && !string.IsNullOrEmpty(custInfo.Email))
            {
                var otpResult = _otpService.GetOTPVerifyinfo(custId, (int)OtpType.Email);
                if (otpResult.Status != 1)
                {
                    // 產生6位數驗證碼
                    var code = _tools.GetRandomNumString(6);
                    var key = _authBLL.ApplyToken(custInfo);
                    var verificationUrl = $"{url}?key={key}&emailcode={code}";

                    var sendMailResult = new ApiResult();

                    if (isUsingMailSMTP)
                    {
                        sendMailResult = await _smtpMailBll.SendMailAsync(custInfo, new { verifyemailurl = verificationUrl }, TemplateType.VerificationMail);
                    }
                    else
                    {
                        sendMailResult = await _sendGridBll.SendMailAsync(custInfo, new { verifyemailurl = verificationUrl }, TemplateType.VerificationMail);
                    }


                    if (sendMailResult.ErrorCode == ErrorCode.Success)
                    {
                        var insertOtpResult = _otpService.OTPVerifyinfoIns(custId, code, (int)OtpType.Email);
                        apiResult.ErrorCode = (ErrorCode)insertOtpResult.Status;
                        apiResult.Data = new SendOTPEmailResult
                        {
                            Status = insertOtpResult.Status,
                            StartUnixTime = insertOtpResult.StartUnixTime,
                            Request_id = insertOtpResult.Request_id
                        };
                    }
                    else
                    {
                        apiResult.ErrorCode = sendMailResult.ErrorCode;
                        apiResult.Message = sendMailResult.Message;
                    }
                }
                else
                {
                    apiResult.ErrorCode = (ErrorCode)(otpResult.Status == 1 ? 0 : 1000);
                    apiResult.Data = new SendOTPEmailResult
                    {
                        Status = otpResult.Status,
                        StartUnixTime = otpResult.StartUnixTime,
                        Request_id = otpResult.Request_id
                    };
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }
            return apiResult;
        }

        public ApiResult VerifyOtp(int custId, string inputOtp)
        {
            var result = _otpService.VerifyOTP(new VerifyOTPReq { CustId = custId, InputOTP = inputOtp }, (int)OtpType.Email);
            var apiResult = new ApiResult
            {
                ErrorCode = (ErrorCode)(result)
            };
            return apiResult;
        }
    }
}