using System.Net;
using System.Web;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class SmsCloudBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, SmsCloudTypeConfig> _config;
        private readonly SelLangBLL _selLangBll;
        private readonly ILogger<OTPSMSService> _logger;
        private readonly SiteLanguageBLL _siteLanguageBll;
        private readonly OTPSMSBLL _oTpSmsBll;

        public SmsCloudBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<OTPSMSService>>();
            _config = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SmsCloudTypeConfig>("SMSCloud");
            _siteLanguageBll = provider.GetRequiredService<SiteLanguageBLL>();
            _oTpSmsBll = provider.GetRequiredService<OTPSMSBLL>();
        }

        public async Task<ApiResult> SendOTP(OTPRequestObject otp)
        {
            var custInfo = otp.CustInfoProfile;
            var phoneNo = custInfo.GetPhoneNo();
            return await SendToSmsCloud(phoneNo, otp.OTPNum, custInfo, custInfo.UserLang, custInfo.CustId);
        }

        private string MakeSmsContext(CustInfoProfile custInfo, string otpNum)
        {
            var userLang = custInfo.UserLang;
            var smsMessage = string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), otpNum);
           
            return smsMessage;
        }

        private async Task<ApiResult> SendToSmsCloud(string phoneNo, string otpNum, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            var config = _config[custInfo.SiteId].OTP.FirstOrDefault(x => x.AreaCode == custInfo.AreaCode);
            var smsMessage = MakeSmsContext(custInfo, otpNum);
            var oTpLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.SmsCloud.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = phoneNo,
                Content = smsMessage,
                OTPNum = otpNum,
                Request_id = Guid.NewGuid().ToString()
            };

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    {"appkey", config.AppKey},
                    {"appcode", config.Appcode},
                    {"appsecret",config.AppSecret},
                    {"phone", phoneNo},
                    {"msg",  smsMessage},
                };
              
                using var httpResponse = await _httpHelper.HttpGetAsync(config.ResourceURL, null, parameters, null , 9090);
                if (httpResponse is { StatusCode: HttpStatusCode.OK })
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<SmsCloudResult>(content);

                    if (response.code.Equals("00000"))
                    {
                        var resultDetail = response.result.FirstOrDefault(x => x.phone == phoneNo);
                        oTpLog.OTPResponse = $"{resultDetail.status} || {resultDetail.desc}";
                        
                        if (resultDetail.status.Equals("00000"))
                        {
                            _logger.LogInformation($"SmsCloud - {phoneNo},{custInfo.UserName},{otpNum},status:{resultDetail.status},reason:{resultDetail.desc},messageId:{response.uid}", $"{custInfo.SiteName}_OTP");
                            oTpLog.OTPStatus = (int)OTPStatus.SendToProvider;
                            apiResult.ErrorCode = ErrorCode.Success;
                        }
                        else
                        {
                            _logger.LogInformation($"SmsCloud - {phoneNo},{custInfo.UserName},{otpNum},status:{resultDetail.status},reason:{resultDetail.desc},messageId:{response.uid}", $"{custInfo.SiteName}_OTP");
                            apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                            apiResult.Message = _siteLanguageBll.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                        }
                    }
                    else
                    {
                        var msg = $"SmsCloud - Error Response Code:{httpResponse.StatusCode} || {content}";
                        _logger.LogInformation(msg);
                        oTpLog.OTPResponse = msg;
                        apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                        apiResult.Message = _siteLanguageBll.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    }
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                    apiResult.Message = _siteLanguageBll.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    var httpErrorMsg = $"SmsCloud - Http error code :{httpResponse.StatusCode}|| {httpResponse.ReasonPhrase}";
                    _logger.LogInformation(httpErrorMsg);
                    oTpLog.OTPResponse = httpErrorMsg;
                }
            }
            catch (Exception ex)
            {
                var exMsg = $"SmsCloud - Exception error :{ex}";
                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                apiResult.Message = _siteLanguageBll.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                apiResult.RawMessage = exMsg;
                _logger.LogError(ex, exMsg);
                oTpLog.OTPResponse = exMsg;
            }

            _oTpSmsBll.InsertOTPLog(oTpLog);
            return apiResult;
        }
    }
}
