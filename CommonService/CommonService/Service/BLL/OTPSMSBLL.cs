using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Service.Library;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;
        private readonly IOptions<Dictionary<string, SkyLineSMSServiceTypeConfig>> SkyLineSMSServiceTypeConfig;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<OTPSMSService> logger;
        private readonly Log2File log2File;

        public OTPSMSBLL(HttpHelper httpHelper, OTPSMSService sOTPSMSService, IOptions<Dictionary<string, SkyLineSMSServiceTypeConfig>> SkyLineSMSServiceTypeConfig, Tools tools, SelLangBLL selLangBLL, ILogger<OTPSMSService> logger, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
            this.SkyLineSMSServiceTypeConfig = SkyLineSMSServiceTypeConfig;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.logger = logger;
            this.log2File = log2File;
        }
        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfo(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.GetOTPVerifyinfo(custId);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;

            return apiResult;
        }
        public ApiResult<SendOTPSMSResult> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;

            return apiResult;
        }

        public ApiResult VerifyOPT(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            int result = sOTPSMSService.VerifyOPT(verifyOTPReq);

            apiResult.ErrorCode = (ErrorCode)(result);
            return apiResult;
        }
        public string MakeSignString(string account, string pwd, string datatime)
        {
            string tmpstr = $"{account}{pwd}{datatime}";
            return tools.ToMD5(tmpstr);

        }
        public async Task<ApiResult<SendOTPSMSResult>> SendSkyLineOTPSMSAsync(string PhoneNo, CustInfoProfile custInfo, int custId, string userLang)
        {
            string OTPNum = tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            ApiResult apiResult1 = await SendOTPSMSAsync(PhoneNo, OTPNum, custInfo, userLang);
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
        public async Task<ApiResult> SendOTPSMSAsync(string PhoneNo, string OTPNum, CustInfoProfile custInfo, string userLang)
        {
            var apiResult = new ApiResult();
            string dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            var Config = SkyLineSMSServiceTypeConfig.Value[custInfo.SiteName].OTP;
            string sign = MakeSignString(Config.Account, Config.Password, dateTime);
            string url = $"{Config.ResourceURL}account={Config.Account}&sign={sign}&datetime={dateTime}";

            string siteName = custInfo.SiteName;
            string smsMessage = $"[{siteName}]{selLangBLL.GetResourceValue("lbl_OTP", userLang)} {OTPNum}";
            //lbl_OTP
            if (custInfo.SiteName.Equals("Bwing", StringComparison.OrdinalIgnoreCase) || custInfo.SiteName.Equals("Goal123", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            }
            else if (userLang.Equals("id", StringComparison.OrdinalIgnoreCase) || PhoneNo.StartsWith("+62"))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", "id"), OTPNum);
            }
            else if (userLang.Equals("hi", StringComparison.OrdinalIgnoreCase))
            {
                smsMessage = string.Format(selLangBLL.GetResourceValue("lbl_OTP", userLang), siteName, OTPNum);
            }

            //String smsMessage = $"{HttpUtility.UrlEncode(selLangBLL.GetResourceValue("lbl_OTP", langId))}{OTPNum}";
            // String smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";

            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", siteName }
            };

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            using var result = await httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            log2File.Info(result.Content.ReadAsStringAsync().Result, $"{siteName}_OTP");

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);


                if (resultResp.status == 0)
                {
                    log2File.Info($"{PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    log2File.Error($"{PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTP");
                    if (resultResp.status == -6 || resultResp.status == -7 || resultResp.status == -8 || resultResp.status == -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                    }
                    else
                    {
                        logger.LogError($"Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.Fail;
                        //apiResult.Message = $"StatusCode = {resultResp.status} Message = {resultResp.reason}";
                    }
                }
            }
            else
            {
                log2File.Error(result.Content.ReadAsStringAsync().Result, $"{siteName}_OTP");
            }

            return apiResult;
        }
        public async Task<ApiResult> SendSMSAsync(string PhoneNo, CustInfoProfile custInfo, string userLang)
        {
            var apiResult = new ApiResult();
            string dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            var Config = SkyLineSMSServiceTypeConfig.Value[custInfo.SiteName].SMS;
            string sign = MakeSignString(Config.Account, Config.Password, dateTime);
            string url = $"{Config.ResourceURL}account={Config.Account}&sign={sign}&datetime={dateTime}";

            string siteName = custInfo.SiteName;
            string smsMessage = $"[{siteName}]{selLangBLL.GetResourceValue("lbl_DepositReject", userLang)}  https://direct.lc.chat/11638088/";
            
            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", siteName }
            };

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            var result = await httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            log2File.Info(result.Content.ReadAsStringAsync().Result, $"{siteName}_DepositRejectSMS");

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);

                if (resultResp.status == 0)
                {
                    log2File.Info($"{PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    log2File.Error($"{PhoneNo},{custInfo.UserName},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_DepositRejectSMS");
                    if (resultResp.status == -6 || resultResp.status == -7 || resultResp.status == -8 || resultResp.status == -9)//號碼錯誤傳5002給前端
                    {
                        apiResult.ErrorCode = ErrorCode.PhoneError;
                    }
                    else
                    {
                        logger.LogError($"Error HttpStatus = {result.StatusCode}\r\nRespstatus ={resultResp.status}\r\nRespMsg = {resultResp.reason}");
                        apiResult.ErrorCode = ErrorCode.Fail;
                    }
                }
            }
            else
            {
                log2File.Error(result.Content.ReadAsStringAsync().Result, $"{siteName}_DepositRejectSMS");
            }
            return apiResult;
        }
    }
}
