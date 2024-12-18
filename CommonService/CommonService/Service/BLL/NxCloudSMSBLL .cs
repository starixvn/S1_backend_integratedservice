using System.Net;

using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class NxCloudSMSBLL
    {
        private readonly IConfiguration _configuration;
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, NxcloudSMSServiceTypeConfig> _config;
        private readonly SelLangBLL _selLangBLL;
        private readonly ILogger<OTPSMSService> _logger;
        private readonly SiteLanguageBLL _siteLanguageBLL;
        private readonly OTPSMSBLL _oTPSMSBLL;

        public NxCloudSMSBLL(IConfiguration configuration, IServiceProvider provider)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<OTPSMSService>>();
            _config = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NxcloudSMSServiceTypeConfig>("NxCloud");
            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
        }

        public async Task<ApiResult> SendOTP(OTPRequestObject otp)
        {
            var custInfo = otp.CustInfoProfile;
            var phoneNo = custInfo.GetPhoneNo();
            return await SendToNxCloud(phoneNo, otp.OTPNum, custInfo, custInfo.UserLang, custInfo.CustId);
        }

        private string MakeSMSContext(CustInfoProfile custInfo, string OTPNum, string PhoneNo)
        {
            var brandName = _configuration.GetSection("BrandName").Get<string>();
            var userLang = custInfo.UserLang;
            var smsMessage = string.Format(_selLangBLL.GetResourceValue("lbl_OTP", userLang), OTPNum);
            var areaCode = custInfo.AreaCode;
            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || areaCode.StartsWith("+62") || PhoneNo.StartsWith("62"))
            {
                smsMessage = string.Format(_selLangBLL.GetResourceValue("lbl_OTP", "id"), OTPNum);
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(_selLangBLL.GetResourceValue("lbl_OTP", custInfo.UserLang), brandName, OTPNum);
            }
            return smsMessage;
        }

        private async Task<ApiResult> SendToNxCloud(string PhoneNo, string OTPNum, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            var brandName = _configuration.GetSection("BrandName").Get<string>();
            var Config = _config[custInfo.SiteId].OTP;
            var smsMessage = MakeSMSContext(custInfo, OTPNum, PhoneNo);
            var oTPLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.NxCloud.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = PhoneNo,
                Content = smsMessage,
                OTPNum = OTPNum
            };

            try
            {
                var data = new Dictionary<string, string>
                {
                    {"appkey", Config.Appkey},
                    {"secretkey", Config.Secretkey},
                    {"phone", PhoneNo},
                    {"content", smsMessage},
                };

                using var httpResponse = await _httpHelper.HttpFromPostAsync(Config.ResourceURL, null, data, "application/x-www-form-urlencoded");
                if (httpResponse is { StatusCode: HttpStatusCode.OK })
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var responsjson = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

                    if (responsjson.TryGetValue("code", out var code))
                    {
                        responsjson.TryGetValue("messageid", out var messageid);
                        responsjson.TryGetValue("result", out var result);

                        oTPLog.Request_id = messageid.ToString();
                        oTPLog.OTPResponse = $"{code} || {result}";

                        if (code.Equals("0"))
                        {
                            _logger.LogInformation($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid}", $"{brandName}_OTP");
                            oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                            apiResult.ErrorCode = ErrorCode.Success;
                        }
                        else
                        {
                            SentrySdk.CaptureMessage($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid},{brandName}_OTP");

                            if (code.Equals("6"))
                            {
                                apiResult.ErrorCode = ErrorCode.PhoneError;
                                oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                            }
                            else
                            {
                                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                                apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                            }
                        }
                    }
                    else
                    {
                        var msg = $"NxCloud - Error HttpStatus:{httpResponse.StatusCode} || {content}";
                        SentrySdk.CaptureMessage(msg);
                        oTPLog.OTPResponse = msg;
                        apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                        apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    }
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                    apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    var httpErrorMsg = $"NxCloud - Http error code :{httpResponse.StatusCode}|| {httpResponse.ReasonPhrase}";
                    _logger.LogInformation(httpErrorMsg);
                    oTPLog.OTPResponse = httpErrorMsg;
                }
            }
            catch (Exception ex)
            {
                var exMsg = $"NxCloud - Exception error :{ex}";
                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                apiResult.RawMessage = exMsg;
                _logger.LogError(ex, exMsg);
                oTPLog.OTPResponse = exMsg;
            }

            _oTPSMSBLL.InsertOTPLog(oTPLog);
            return apiResult;
        }

    }
}
