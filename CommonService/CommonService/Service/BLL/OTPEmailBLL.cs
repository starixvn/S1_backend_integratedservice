using System.Threading.Tasks;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class OTPEmailBLL
    {
        private readonly SendGridBLL _sendGridBll;
        private readonly OTPSMSService _otpService;
        private readonly CustInfoService _custInfoService;
        private readonly Tools _tools;

        public OTPEmailBLL(SendGridBLL sendGridBll, OTPSMSService otpService, Tools tools, CustInfoService custInfoService)
        {
            
            _sendGridBll = sendGridBll;
            _otpService = otpService;
            _tools = tools;
            _custInfoService = custInfoService;
        }
        
        public async Task<ApiResult<SendOTPEmailResult>> SendOtpEmailAsync(int custId, string url)
        {
            var apiResult = new ApiResult<SendOTPEmailResult>();
            var custInfo = _custInfoService.GetCustInfoProfile(custId);
            if (custInfo != null && !string.IsNullOrEmpty(custInfo.Email))
            { 
                var otpResult = _otpService.GetOTPVerifyinfo(custId);
                if (otpResult.Status != 1)
                {
                    // 產生6位數驗證碼
                    var code = _tools.GetRandomNumString(6);
                    var verificationUrl = $"{url}?code={code}";
                    var sendMailResult = await _sendGridBll.SendMailAsync(custInfo, new {resetpwdurl=verificationUrl}, TemplateType.VerificationMail);
                    
                    if (sendMailResult.ErrorCode == ErrorCode.Success)
                    {
                        var insertOtpResult = _otpService.OTPVerifyinfoIns(custId, code);
                        apiResult.ErrorCode = (ErrorCode)insertOtpResult.Status;
                        apiResult.Data = new SendOTPEmailResult()
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
                    apiResult.Data = new SendOTPEmailResult()
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
    }
}