using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class OTPEmailBLL
    {
        private readonly OTPSMSService _otpService;
        private readonly Tools _tools;
        private readonly AuthBLL _authBLL;

        private readonly TemplateType template = TemplateType.VerificationMail;
        private readonly Dictionary<int, MailStatusSetting> _mailSettingConfig;
        private readonly CustInfoBLL _custInfoBll;

        private readonly SendGridBLL _sendGridBll;
        private readonly IBMBLL _iBMBll;

        public OTPEmailBLL(IServiceProvider provider)
        {
            _otpService = provider.GetRequiredService<OTPSMSService>();
            _tools = provider.GetRequiredService<Tools>();
            _authBLL = provider.GetRequiredService<AuthBLL>();

            _mailSettingConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MailStatusSetting>("StatusSetting");
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();

            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _iBMBll = provider.GetRequiredService<IBMBLL>();
        }

        public async Task<ApiResult<SendOTPEmailResult>> SendOtpEmailAsync(int custId, string url)
        {
            var apiResult = new ApiResult<SendOTPEmailResult>();
            var custInfo = _custInfoBll.GetCustInfoProfile(custId);

            if (!_mailSettingConfig.TryGetValue(custInfo.SiteId, out var config))
            {
                return new ApiResult<SendOTPEmailResult>
                {
                    ErrorCode = ErrorCode.SettingNotFound,
                    Message = "MailSettingConfig not found."
                };
            }

            if (!ParameterCheck(custInfo))
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
                return apiResult;
            }

            var otpResult = _otpService.GetOTPVerifyinfo(custId, (int)OtpType.Email);

            if (otpResult.Status == 1)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = "already gen vaild otp before.";
                apiResult.Data = new SendOTPEmailResult
                {
                    Status = otpResult.Status,
                    StartUnixTime = otpResult.StartUnixTime,
                    Request_id = otpResult.Request_id
                };
                return apiResult;
            }

            var verificationUrl = GenVerificationUrl(custInfo, url, out string code);

            var sendMailResult = await SendMailAsync(config, custInfo, verificationUrl);

            if (sendMailResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = sendMailResult.ErrorCode;
                apiResult.Message = sendMailResult.Message;
                return apiResult;
            }

            var insertOtpResult = _otpService.OTPVerifyinfoIns(custId, code, (int)OtpType.Email);
            apiResult.ErrorCode = (ErrorCode)insertOtpResult.Status;
            apiResult.Data = new SendOTPEmailResult
            {
                Status = insertOtpResult.Status,
                StartUnixTime = insertOtpResult.StartUnixTime,
                Request_id = insertOtpResult.Request_id
            };
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

        private bool ParameterCheck(CustInfoProfile custInfo)
        {
            if (string.IsNullOrWhiteSpace(custInfo?.UserName)
                || string.IsNullOrWhiteSpace(custInfo?.Email))
                return false;

            return true;
        }

        private string GenVerificationUrl(CustInfoProfile custInfo, string url, out string code)
        {
            // 產生6位數驗證碼(SendGrid做法，IBM不確定會不會一樣)
            code = _tools.GetRandomNumString(6);
            var key = _authBLL.ApplyToken(custInfo);
            return $"{url}?key={key}&emailcode={code}";
        }

        private async Task<ApiResult> SendMailAsync(MailStatusSetting config, CustInfoProfile custInfo, string verificationUrl)
        {
            switch (config.Mode)
            {
                case (int)MailType.SendGrid:
                    return await _sendGridBll.SendMailAsync(custInfo, new { verifyemailurl = verificationUrl }, template);
                case (int)MailType.IBM:
                    return await _iBMBll.SendMailAsync(custInfo, new { verifyemailurl = verificationUrl }, template);
                default:
                    return new ApiResult
                    {
                        ErrorCode = ErrorCode.SettingNotFound,
                        Message = "MailType not define."
                    };
            }
        }
    }
}