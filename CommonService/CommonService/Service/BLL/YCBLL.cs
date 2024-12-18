using System.Net;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class YCBLL
    {
        private readonly IConfiguration _configuration;
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, YcSmsServiceTypeConfig> _config;
        private readonly ILogger<YCBLL> _logger;
        private readonly SiteLanguageBLL _siteLanguageBLL;
        private readonly OTPSMSBLL _oTPSMSBLL;

        public YCBLL(IConfiguration configuration, IServiceProvider provider)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<YCBLL>>();
            _config = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YcSmsServiceTypeConfig>("YcSMS");
            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
        }

        public async Task<ApiResult> SendOTP(OTPRequestObject otp)
        {
            var custInfo = otp.CustInfoProfile;
            var phoneNo = custInfo.PhoneNo.StartsWith("0") ? custInfo.PhoneNo.Substring(1) : custInfo.PhoneNo;
            var phone = $"86{phoneNo}";
            return await SendToYC(phone, otp.OTPNum, custInfo, custInfo.CustId, custInfo.UserLang);
        }

        private async Task<ApiResult> SendToYC(string phone, string otpNum, CustInfoProfile custInfo, int custId, string userLang)
        {
            var brandName = _configuration.GetSection("BrandName").Get<string>();
            var apiResult = new ApiResult();
            var config = _config[custInfo.SiteId].OTP;
            var postData = new Dictionary<string, string>
            {
                {"merchant", config.Merchant},
                {"token", config.Token},
                {"phone", phone},
                {"content", otpNum},
            };
            var oTPLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.YC.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = phone,
                Content = otpNum,
                OTPNum = otpNum
            };

            using var responseResult = await _httpHelper.HttpFromPostAsync(config.ApiPath, null, postData, "application/x-www-form-urlencoded");
            var responseBody = await responseResult.Content.ReadAsStringAsync();

            if (responseResult.StatusCode == HttpStatusCode.OK)
            {
                var response = JsonConvert.DeserializeObject<YcSmsSendOtpResult>(responseBody);
                oTPLog.OTPResponse = $"{response.status} || {response.error_code} || {response.res}";
                oTPLog.Request_id = Guid.NewGuid().ToString();//YC沒提供Request_id，自行建立
                if (response?.status == "success")
                {
                    _logger.LogInformation($"YCOTP Success - {phone},{custInfo.UserName},{otpNum}, responseBody：{responseBody}, {brandName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                }
                else
                {
                    _logger.LogInformation($"YCOTP Fail - {phone},{custInfo.UserName},{otpNum}, responseBody：{responseBody}, {brandName}_OTP");
                    apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                    apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                _logger.LogInformation($"YCOTP HttpRequest Fail -{phone},{custInfo.UserName},{otpNum}, HttpStatusCode：{responseResult.StatusCode} , responseBody：{responseBody}, {brandName}_OTP");
                oTPLog.OTPResponse = $"{responseResult.StatusCode} || {responseResult.ReasonPhrase}";
            }

            _oTPSMSBLL.InsertOTPLog(oTPLog);
            return apiResult;
        }
    }
}
