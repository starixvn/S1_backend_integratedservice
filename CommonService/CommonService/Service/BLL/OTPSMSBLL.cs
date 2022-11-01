using System.Net;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Service.Library;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly OTPSMSService _sOtpSmsService;
        private readonly IOptions<Dictionary<string, SkyLineSMSServiceTypeConfig>> _skyLineSmsServiceTypeConfig;
        private readonly IOptions<Dictionary<string, YcSmsServiceTypeConfig>> _YcSmsServiceTypeConfig;
        private readonly Tools _tools;
        private readonly SelLangBLL _selLangBll;
        private readonly ILogger<OTPSMSService> _logger;
        private readonly Log2File _log2File;
        private readonly AWSSNSHelper _awsSnsHelper;
       
        public OTPSMSBLL(
            HttpHelper httpHelper, 
            OTPSMSService sOtpSmsService, 
            IOptions<Dictionary<string, SkyLineSMSServiceTypeConfig>> skyLineSmsServiceTypeConfig,
            IOptions<Dictionary<string, YcSmsServiceTypeConfig>> ycSmsServiceTypeConfig, 
            Tools tools, 
            SelLangBLL selLangBll, 
            ILogger<OTPSMSService> logger, 
            Log2File log2File, AWSSNSHelper awsSnsHelper
            )
        {
            _httpHelper = httpHelper;
            _sOtpSmsService = sOtpSmsService;
            _skyLineSmsServiceTypeConfig = skyLineSmsServiceTypeConfig;
            _YcSmsServiceTypeConfig = ycSmsServiceTypeConfig;
            _tools = tools;
            _selLangBll = selLangBll;
            _logger = logger;
            _log2File = log2File;
            _awsSnsHelper = awsSnsHelper;
        }

        #region Common

        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _sOtpSmsService.GetOTPVerifyinfo(custId);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;

            return apiResult;
        }

        private ApiResult<SendOTPSMSResult> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = _sOtpSmsService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }
        
        public ApiResult VerifyOPT(VerifyOTPReq verifyOtpReq)
        {
            var apiResult = new ApiResult();
            var result = _sOtpSmsService.VerifyOTP(verifyOtpReq);

            apiResult.ErrorCode = (ErrorCode)(result);
            return apiResult;
        }
        
        private string MakeSignString(string account, string pwd, string dataTime)
        {
            var tmpStr = $"{account}{pwd}{dataTime}";
            return _tools.ToMD5(tmpStr);
        }
        
        #endregion

        #region SkyLine

        public async Task<ApiResult<SendOTPSMSResult>> SendSkyLineOTPSMSAsync(string PhoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            var OTPNum = _tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var apiResult1 = await SendSkyLineOTPSMSAsync(PhoneNo, OTPNum, custInfo, userLang, custId);
            var apiResultSendOTP = apiResult1;
            //if send SMS Success
            if (apiResultSendOTP.ErrorCode == ErrorCode.Success)
            {
                apiResult = OTPVerifyinfoInsAsync(custId, OTPNum);
            }
            else
            {
                apiResult.ErrorCode = apiResultSendOTP.ErrorCode;
                apiResult.Message = apiResultSendOTP.Message;
            }
            return apiResult;
        }

        private async Task<ApiResult> SendSkyLineOTPSMSAsync(string PhoneNo, string OTPNum, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            string areaCode = custInfo.AreaCode;
            string dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            var Config = _skyLineSmsServiceTypeConfig.Value[custInfo.SiteName].OTP;
            string sign = MakeSignString(Config.Account, Config.Password, dateTime);
            string url = $"{Config.ResourceURL}account={Config.Account}&sign={sign}&datetime={dateTime}";

            string siteName = custInfo.SiteName;
            string smsMessage = $"[{siteName}]{string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), OTPNum)}";

            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || PhoneNo.StartsWith("+62"))
            {
                smsMessage = string.Format(_selLangBll.GetResourceValue("lbl_OTP", "id"), OTPNum);
            }
            else if (areaCode.StartsWith("+91") || PhoneNo.StartsWith("91"))//skyline指定
            {
                smsMessage = $"Your Verification Code is {OTPNum},Valid for 2 minutes.";
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), siteName, OTPNum);
            }

            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", siteName }
            };

            var oTPLog = new OTPLog()
            {
                OTPStatus = (int)OTPStatus.Fail,
                OTPType = (int)OtpType.SMS,
                OTPProvider = OTPProvider.SkyLine.ToString(),
                CurrencyId = custInfo.CurrencyId,
                CustId = custId,
                UserName = custInfo.UserName,
                PhoneNo = PhoneNo,
                Content = smsMessage,
                OTPNum = OTPNum
            };

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            using var result = await _httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            _log2File.Info($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_OTP");

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);              
                oTPLog.OTPResponse = $"{resultResp.status} || {resultResp.reason}";

                if (resultResp.status == 0)
                {
                    _log2File.Info($"SkyLine - {PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                    oTPLog.Request_id = GetRequestid(resultResp);
                }
                else
                {
                    _log2File.Error($"SkyLine - {PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    if (resultResp.status == -6 || resultResp.status == -7 || resultResp.status == -8 || resultResp.status == -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                        oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                    }
                    else
                    {
                        _logger.LogError($"SkyLine - Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
            }
            else
            {
                _log2File.Error($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_OTP");
                oTPLog.OTPResponse = $"SkyLine - Error HttpStatusCode : {result.StatusCode} || {result.ReasonPhrase}";
            }

            _sOtpSmsService.InsertOTPLog(oTPLog);
            return apiResult;
        }
       
        public async Task<ApiResult> SendSkyLineSMSAsync(string PhoneNo, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            string dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            var Config = _skyLineSmsServiceTypeConfig.Value[custInfo.SiteName].SMS;
            string sign = MakeSignString(Config.Account, Config.Password, dateTime);
            string url = $"{Config.ResourceURL}account={Config.Account}&sign={sign}&datetime={dateTime}";

            string siteName = custInfo.SiteName;
            string smsMessage = $"[{siteName}]{_selLangBll.GetResourceValue("lbl_DepositReject", userLang)}  https://direct.lc.chat/11638088/";
            
            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", siteName }
            };
            var oTPLog = new OTPLog()
            {
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

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            var result = await _httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            _log2File.Info($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");

            if (result.StatusCode == HttpStatusCode.OK)
            {
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);        
                oTPLog.OTPResponse = $"{resultResp.status} || {resultResp.reason}";

                if (resultResp.status == 0)
                {
                    _log2File.Info($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                    oTPLog.Request_id = GetRequestid(resultResp);
                }
                else
                {
                    _log2File.Error($"SkyLine - {PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    if (resultResp.status == -6 || resultResp.status == -7 || resultResp.status == -8 || resultResp.status == -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                        oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                    }
                    else
                    {
                        _logger.LogError($"SkyLine - Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
            }
            else
            {
                _log2File.Error($"SkyLine - {result.Content.ReadAsStringAsync().Result}", $"{siteName}_DepositRejectSMS");
                oTPLog.OTPResponse = $"SkyLine - Error HttpStatus :{result.StatusCode} || {result.ReasonPhrase}";
            }

            _sOtpSmsService.InsertOTPLog(oTPLog);
            return apiResult;
        }

        #endregion

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
        
        #region YC元澄 SMS 平台

        public async Task<ApiResult<SendOTPSMSResult>> SendYcOTPMSAsync(string phoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            var otpNum = _tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var responseResult = await SendToYcOTPSMSAsync(phoneNo, otpNum, custInfo, custId, userLang);

            //if send SMS Success
            if (responseResult.ErrorCode == ErrorCode.Success)
            {
                apiResult = OTPVerifyinfoInsAsync(custId, otpNum);
            }
            else
            {
                apiResult.ErrorCode = responseResult.ErrorCode;
                apiResult.Message = responseResult.Message;
            }
            return apiResult;
        }

        private async Task<ApiResult> SendToYcOTPSMSAsync(string phoneNo, string otpNum, CustInfoProfile custInfo, int custId, string userLang = "")
        {
            var apiResult = new ApiResult();
            var config = _YcSmsServiceTypeConfig.Value[custInfo.SiteName].OTP;
            var siteName = custInfo.SiteName;
            var phone = $"86{phoneNo}";
            var postData = new Dictionary<string, string>()
            {
                {"merchant", config.Merchant},
                {"token", config.Token},
                {"phone", phone},
                {"content", otpNum},
            };
            var oTPLog = new OTPLog()
            {
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
                    _logger.LogInformation($"YCOTP Success - {phoneNo},{custInfo.UserName},{otpNum}, responseBody：{responseBody}, {siteName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Success;
                    oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                }
                else
                {
                    _logger.LogError($"YCOTP Fail - {phoneNo},{custInfo.UserName},{otpNum}, responseBody：{responseBody}, {siteName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                _logger.LogError($"YCOTP HttpRequest Fail -{phoneNo},{custInfo.UserName},{otpNum}, HttpStatusCode：{responseResult.StatusCode} , responseBody：{responseBody}, {siteName}_OTP");
                oTPLog.OTPResponse = $"{responseResult.StatusCode} || {responseResult.ReasonPhrase}";
            }

            _sOtpSmsService.InsertOTPLog(oTPLog);
            return apiResult;
        }

        #endregion
        
        #region AWS SMS
        public async Task<ApiResult<SendOTPSMSResult>> SendAwsOtpAsync(string phoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var otpNum = _tools.GetRandomNumString(6);
            var message = string.Format(_selLangBll.GetResourceValue("lbl_OTP", userLang), otpNum);
            var responseResult = await SendToAwsOtpAsync(phoneNo, otpNum, message, custInfo, custId, userLang);

            //if send SMS Success
            if (responseResult.ErrorCode == ErrorCode.Success)
            {
                apiResult = OTPVerifyinfoInsAsync(custId, otpNum);
            }
            else
            {
                apiResult.ErrorCode = responseResult.ErrorCode;
                apiResult.Message = responseResult.Message;
            }
            return apiResult;
        }
       
        
        private async Task<ApiResult> SendToAwsOtpAsync(string phoneNo, string otpNum, string message, CustInfoProfile custInfo, int custId)
        {
            var apiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Fail
            };
            
            var request = new PublishRequest
            {
                PhoneNumber = $"+{phoneNo}",
                Message = message,
            };
            
            var oTpLog = new OTPLog()
            {
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
            
            _sOtpSmsService.InsertOTPLog(oTpLog);
            return apiResult;
        }
        #endregion
    }
}
