using System.Net;

using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class SkyLineBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, SkyLineSMSServiceTypeConfig> _config;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly ILogger<OTPSMSService> _logger;
        private readonly SiteLanguageBLL _siteLanguageBLL;
        private readonly OTPSMSBLL _oTPSMSBLL;

        public SkyLineBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _tools = provider.GetRequiredService<Tools>();
            _selLangBll = provider.GetRequiredService<SelLangBLL>();
            _logger = provider.GetRequiredService<ILogger<OTPSMSService>>();
            _config = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SkyLineSMSServiceTypeConfig>("SkyLineSMS");
            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
            _oTPSMSBLL = provider.GetRequiredService<OTPSMSBLL>();
        }

        public async Task<ApiResult> SendSMS(string PhoneNo, CustInfoProfile custInfo, string userLang, int custId)
        {
            //var apiResult = new ApiResult();
            //var dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            //var Config = _config[custInfo.SiteId].SMS;
            //var sign = MakeSignString(Config.Account, Config.Password, dateTime);
            //var url = $"https://api.itniotech.com/sms/sendSms/?account=xNGyNJBp&sign={sign}&datetime={dateTime}";

            //var siteName = custInfo.SiteName;
            //var smsMessage = $"[{siteName}]{_selLangBll.GetResourceValue("lbl_DepositReject", userLang)}  https://direct.lc.chat/11638088/";

            //var AcknowledgeVerifySMS = new Dictionary<string, string>
            //{
            //    { "content", smsMessage },
            //    { "numbers", PhoneNo},
            //    { "senderid", siteName }
            //};
            //var oTPLog = new OTPLog
            //{
            //    SiteId = custInfo.SiteId,
            //    OTPStatus = (int)OTPStatus.Fail,
            //    OTPType = (int)OtpType.SMS,
            //    OTPProvider = OTPProvider.SkyLine.ToString(),
            //    CurrencyId = custInfo.CurrencyId,
            //    CustId = custId,
            //    UserName = custInfo.UserName,
            //    PhoneNo = PhoneNo,
            //    Content = smsMessage,
            //    OTPNum = ""
            //};

            //var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            //var result = await _httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            //_logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");

            //if (result.StatusCode == HttpStatusCode.OK)
            //{
            //    var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);
            //    oTPLog.OTPResponse = $"{resultResp.status} || {resultResp.reason}";

            //    if (resultResp.status == 0)
            //    {
            //        _logger.LogInformation($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
            //        apiResult.ErrorCode = ErrorCode.Success;
            //        oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
            //        oTPLog.Request_id = GetRequestid(resultResp);
            //    }
            //    else
            //    {
            //        _logger.LogInformation($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
            //        if (resultResp.status is -6 or -7 or -8 or -9)//號碼錯誤傳5002給前端
            //        {
            //            apiResult.ErrorCode = ErrorCode.PhoneError;
            //            oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
            //        }
            //        else
            //        {
            //            _logger.LogInformation($"SkyLine - Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
            //            apiResult.ErrorCode = ErrorCode.SMSProviderFail;
            //            apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
            //        }
            //    }
            //}
            //else
            //{
            //    apiResult.ErrorCode = ErrorCode.SMSProviderFail;
            //    apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
            //    _logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");
            //    oTPLog.OTPResponse = $"SkyLine - Error HttpStatus :{result.StatusCode} || {result.ReasonPhrase}";
            //}

            //_oTPSMSBLL.InsertOTPLog(oTPLog);
            //return apiResult;

            var apiResult = new ApiResult();
            var timestamp = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            var config = _config[custInfo.SiteId].OTP;
            var sign = MakeSignString(config.ApiKey, config.ApiSecret, timestamp);

            var siteName = custInfo.SiteName;

            var smsMessage = $"[{siteName}]{_selLangBll.GetResourceValue("lbl_DepositReject", userLang)}  https://direct.lc.chat/11638088/";

            var headerParams = new Dictionary<string, string>
            {
                { "Sign", sign },
                { "Timestamp", timestamp.ToString() },
                { "Api-Key", config.ApiKey }
            };

            var acknowledgeVerifySms = new Dictionary<string, string>
            {
                { "appId", config.Account },
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderId", siteName }
            };

            var oTPLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.SkyLine.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = PhoneNo,
                Content = smsMessage,
                OTPNum = ""
            };

            var parameters = JsonConvert.SerializeObject(acknowledgeVerifySms);
            using var result = await _httpHelper.HttpPost1Async(config.ResourceURL, headerParams, parameters, "application/json");
            _logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var response = result.Content.ReadAsStringAsync().Result;
                var resultResp = JsonConvert.DeserializeObject<SKYLineOTPResult>(response);
                var responseDetail = resultResp.array.FirstOrDefault(x => x.number == PhoneNo);
                oTPLog.OTPResponse = $"{resultResp.status} || {resultResp.reason}";

                if (resultResp.status == 0)
                {
                    _logger.LogInformation($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                    oTPLog.Request_id = responseDetail.msgId;
                }
                else
                {
                    _logger.LogInformation($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    if (resultResp.status is -6 or -7 or -8 or -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                        oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                    }
                    else
                    {
                        _logger.LogInformation($"SkyLine - Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                        apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    }
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                _logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");
                oTPLog.OTPResponse = $"SkyLine - Error HttpStatusCode : {result.StatusCode} || {result.ReasonPhrase}";
            }

            _oTPSMSBLL.InsertOTPLog(oTPLog);
            return apiResult;
        }

        public async Task<ApiResult> SendOTP(OTPRequestObject otp)
        {
            var custInfo = otp.CustInfoProfile;
            var phoneNo = custInfo.GetPhoneNo();
            return await SendToSkyLine(phoneNo, otp.OTPNum, custInfo, custInfo.UserLang, custInfo.CustId);
        }

        private async Task<ApiResult> SendToSkyLine(string phoneNo, string otpNum, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            var areaCode = custInfo.AreaCode;
            var timestamp = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            var config = _config[custInfo.SiteId].OTP;
            var sign = MakeSignString(config.ApiKey, config.ApiSecret, timestamp);

            var siteName = custInfo.SiteName;

            #region message

            var smsMessage = $"[{siteName}]{string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), otpNum)}";
            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {otpNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || phoneNo.StartsWith("+62"))
            {
                smsMessage = string.Format(_selLangBll.GetResourceValue("lbl_OTP", "id"), otpNum);
            }
            else if (areaCode.StartsWith("+91") || phoneNo.StartsWith("91"))//skyline指定
            {
                smsMessage = $"Your Verification Code is {otpNum},Valid for 2 minutes.";
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), siteName, otpNum);
            }

            #endregion
            
            var headerParams = new Dictionary<string, string>
            {
                { "Sign", sign },
                { "Timestamp", timestamp.ToString() },
                { "Api-Key", config.ApiKey }
            };
            
            var acknowledgeVerifySms = new Dictionary<string, string>
            {
                { "appId", config.Account },
                { "content", smsMessage },
                { "numbers", phoneNo},
                { "senderId", siteName }
            };
            
            var oTPLog = new OTPLog
            {
                SiteId = custInfo.SiteId,
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.SkyLine.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = phoneNo,
                Content = smsMessage,
                OTPNum = otpNum
            };

            var parameters = JsonConvert.SerializeObject(acknowledgeVerifySms);
            using var result = await _httpHelper.HttpPost1Async(config.ResourceURL, headerParams, parameters, "application/json");
            _logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_OTP");

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var response = result.Content.ReadAsStringAsync().Result;
                var resultResp = JsonConvert.DeserializeObject<SKYLineOTPResult>(response);
                var responseDetail = resultResp.array.FirstOrDefault(x => x.number == phoneNo);
                oTPLog.OTPResponse = $"{resultResp.status} || {resultResp.reason}";

                if (resultResp.status == 0)
                {
                    _logger.LogInformation($"SkyLine - {phoneNo},{custInfo.UserName},{otpNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                    oTPLog.Request_id = responseDetail.msgId;
                }
                else
                {
                    _logger.LogInformation($"SkyLine - {phoneNo},{custInfo.UserName},{otpNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    if (resultResp.status is -6 or -7 or -8 or -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                        oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                    }
                    else
                    {
                        _logger.LogInformation($"SkyLine - Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                        apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                    }
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.SMSProviderFail;
                apiResult.Message = _siteLanguageBLL.GetLanguageValue(custInfo.SiteId, ErrorCode.SMSProviderFail.ToString(), userLang);
                _logger.LogInformation($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_OTP");
                oTPLog.OTPResponse = $"SkyLine - Error HttpStatusCode : {result.StatusCode} || {result.ReasonPhrase}";
            }

            _oTPSMSBLL.InsertOTPLog(oTPLog);
            return apiResult;
        }

        //sms
        private string MakeSignString(string account, string pwd, string dataTime)
        {
            var tmpStr = $"{account}{pwd}{dataTime}";
            return _tools.ToMD5(tmpStr);
        }

        //otp
        private string MakeSignString(string key, string secret, long dataTime)
        {
            var tmpStr = $"{key}{secret}{dataTime}";
            return _tools.ToMD5(tmpStr);
        }
        
        private string GetRequestid(SKYLineSMSResult sKYLineSMSResult)
        {
            try
            {
                return sKYLineSMSResult.array[0][0];
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
