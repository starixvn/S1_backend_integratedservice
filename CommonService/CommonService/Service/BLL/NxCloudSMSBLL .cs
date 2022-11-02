using System.Net;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Service.Library;

namespace CommonService.Service.BLL
{
    public class NxCloudSMSBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;
        private readonly IOptions<Dictionary<string, NxcloudSMSServiceTypeConfig>> NxcloudSMSServiceTypeConfig;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<OTPSMSService> logger;

        public NxCloudSMSBLL(HttpHelper httpHelper, OTPSMSService sOTPSMSService, IOptions<Dictionary<string, NxcloudSMSServiceTypeConfig>> NxcloudSMSServiceTypeConfig, Tools tools, SelLangBLL selLangBLL, ILogger<OTPSMSService> logger, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
            this.NxcloudSMSServiceTypeConfig = NxcloudSMSServiceTypeConfig;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.logger = logger;
        }

        public ApiResult<SendOTPSMSResult> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var result = sOTPSMSService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }

        public ApiResult VerifyOPT(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            var result = sOTPSMSService.VerifyOTP(verifyOTPReq);

            apiResult.ErrorCode = (ErrorCode)(result);
            return apiResult;
        }
        public async Task<ApiResult<SendOTPSMSResult>> SendOTPSMSAsync(string PhoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            var OTPNum = tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var apiResult1 = await SendOTPSMSAsync(PhoneNo, OTPNum, custInfo, userLang, custId);
            var apiResultSendOTP = apiResult1;
            //if send SMS Success
            if (apiResultSendOTP.ErrorCode == ErrorCode.Success)
            {
                apiResult = OTPVerifyinfoInsAsync(custId, OTPNum);
                //apiResult.ErrorCode = 0;
            }
            else
            {
                apiResult.ErrorCode = apiResultSendOTP.ErrorCode;
                apiResult.Message = apiResultSendOTP.Message;
            }
            return apiResult;
        }
        public string MakeSMSContext(CustInfoProfile custInfo, string OTPNum, string PhoneNo)
        {
            var siteName = custInfo.SiteName;
            var userLang = custInfo.UserLang;
            var smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", userLang), OTPNum);
            var areaCode = custInfo.AreaCode;
            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || areaCode.StartsWith("+62") || PhoneNo.StartsWith("62"))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", "id"), OTPNum);
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", custInfo.UserLang), siteName, OTPNum);
            }
            return smsMessage;
        }

        public async Task<ApiResult> SendOTPSMSAsync(string PhoneNo, string OTPNum, CustInfoProfile custInfo, string userLang, int custId)
        {
            var apiResult = new ApiResult();
            var Config = NxcloudSMSServiceTypeConfig.Value[custInfo.SiteName].OTP;
            var smsMessage = MakeSMSContext(custInfo, OTPNum, PhoneNo);
            var oTPLog = new OTPLog()
            {
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
                var data = new Dictionary<string, string>()
                {
                    {"appkey", Config.Appkey},
                    {"secretkey", Config.Secretkey},
                    {"phone", PhoneNo},
                    {"content", smsMessage},
                };              

                using var httpResponse = await httpHelper.HttpFromPostAsync(Config.ResourceURL, null, data, "application/x-www-form-urlencoded");
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
                            logger.LogInformation($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid}", $"{custInfo.SiteName}_OTP");
                            oTPLog.OTPStatus = (int)OTPStatus.SendToProvider;
                            apiResult.ErrorCode = ErrorCode.Success;
                        }
                        else
                        {
                            apiResult.ErrorCode = ErrorCode.Fail;
                            if (code.Equals("6"))
                            {
                                apiResult.ErrorCode = ErrorCode.PhoneError;
                                oTPLog.OTPResponse = ErrorCode.PhoneError.ToString();
                            }
                            logger.LogInformation($"NxCloud - {PhoneNo},{custInfo.UserName},{OTPNum},status:{code},reason:{result},messageid:{messageid}", $"{custInfo.SiteName}_OTP");
                        }
                    }
                    else
                    {
                        var msg = $"NxCloud - Error HttpStatus:{httpResponse.StatusCode} || {content}";
                        logger.LogInformation(msg);
                        oTPLog.OTPResponse = msg;
                    }
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    var httpErrorMsg = $"NxCloud - Http error code :{httpResponse.StatusCode}|| {httpResponse.ReasonPhrase}";
                    logger.LogInformation(httpErrorMsg);
                    oTPLog.OTPResponse = httpErrorMsg;
                }
            }
            catch (Exception ex)
            {
                var exMsg = $"NxCloud - Exception error :{ex}";
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.RawMessage = exMsg;
                logger.LogError(exMsg);
                oTPLog.OTPResponse = exMsg;
            }

            sOTPSMSService.InsertOTPLog(oTPLog);
            return apiResult;
        }
        
    }
}
