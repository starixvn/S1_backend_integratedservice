using System.Net;

using Amazon.SimpleNotificationService.Model;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class AWSBLL
    {
        private readonly SelLangBLL _selLangBll;
        private readonly ILogger<AWSBLL> _logger;
        private readonly SiteLanguageBLL _siteLanguageBLL;
        private readonly OTPSMSBLL _oTPSMSBLL;
        private readonly AWSSNSHelper _awsSnsHelper;

        public AWSBLL(IServiceProvider provider)
        {
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<AWSBLL>>();
            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
            _awsSnsHelper = provider.GetRequiredService<AWSSNSHelper>();
        }

        public async Task<ApiResult> SendOTP(OTPRequestObject otp)
        {
            var custInfo = otp.CustInfoProfile;
            var phoneNo = $"+{custInfo.GetPhoneNo()}";

            var message = string.Format(_selLangBll.GetResourceValue("lbl_OTP", custInfo.UserLang), otp.OTPNum);
            return await SendToAWS(phoneNo, otp.OTPNum, message, custInfo, custInfo.CustId);
        }

        private async Task<ApiResult> SendToAWS(string phoneNo, string otpNum, string message, CustInfoProfile custInfo, int custId)
        {
            var apiResult = new ApiResult
            {
                ErrorCode = ErrorCode.SMSProviderFail,
                Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), custInfo.UserLang)
            };

            var request = new PublishRequest
            {
                PhoneNumber = phoneNo,
                Message = message,
            };

            var oTpLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.AWS.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = phoneNo,
                Content = message,
                OTPNum = otpNum
            };

            try
            {
                var response = await _awsSnsHelper.PublishAsync(request);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Message = ErrorCode.Success.ToString();
                    oTpLog.OTPStatus = (int)OTPStatus.SendToProvider;
                }

                oTpLog.OTPResponse = $"{response.HttpStatusCode} || {apiResult.ErrorCode}";
                oTpLog.Request_id = response.MessageId;
                _logger.LogInformation($"AWS OTP {(ErrorCode)Enum.ToObject(typeof(ErrorCode), apiResult.ErrorCode)} - {request.PhoneNumber},{custInfo.UserName},{request.Message}, " +
                                       $"MessageId={response.MessageId}, StatusCode={response.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AWS OTP Fail - {request.PhoneNumber},{custInfo.UserName},{request.Message}");
            }

            _oTPSMSBLL.InsertOTPLog(oTpLog);
            return apiResult;
        }
    }
}
