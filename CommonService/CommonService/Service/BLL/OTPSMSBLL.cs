using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Service.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CommonService.Service.BLL
{
    public class OTPSMSBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;
        private readonly IOptions<Dictionary<string, OTPSMSServiceConfig>> OTPSMSServiceConfigList;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;
        private readonly ILogger<OTPSMSService> logger;
        private readonly Log2File log2File;

        public OTPSMSBLL(HttpHelper httpHelper, OTPSMSService sOTPSMSService, IOptions<Dictionary<string, OTPSMSServiceConfig>> OTPSMSServiceConfigList, Tools tools, SelLangBLL selLangBLL, ILogger<OTPSMSService> logger, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
            this.OTPSMSServiceConfigList = OTPSMSServiceConfigList;
            this.tools = tools;
            this.selLangBLL = selLangBLL;
            this.logger = logger;
            this.log2File = log2File;

        }
        public ApiResult<SendOTPSMSResult> GetOTPVerifyInfoAsync(int custId)
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

        public ApiResult VerifyOPTAsync(VerifyOTPReq verifyOTPReq)
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
        public async Task<ApiResult<SendOTPSMSResult>> SendSkyLineOTPSMSAsync(string PhoneNo, CustInfoProfile custInfo, int custId)
        {
            string OTPNum = tools.GetRandomNumString(6);
            var apiResult = new ApiResult<SendOTPSMSResult>();
            ApiResult apiResult1 = await SendOTPSMSAsync(PhoneNo, OTPNum, custInfo);
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


        public async Task<ApiResult> SendOTPSMSAsync(string PhoneNo, string OTPNum, CustInfoProfile custInfo)
        {
            var apiResult = new ApiResult();
            string dateTime = new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyyMMddHHmmss");
            var Config = OTPSMSServiceConfigList.Value[custInfo.SiteName];
            string sign = MakeSignString(Config.Account, Config.Password, dateTime);
            string url = $"{Config.ResourceURL}account={Config.Account}&sign={sign}&datetime={dateTime}";

            //String smsMessage = $"{selLangBLL.GetResourceValue("lbl_OTP", langId)} {OTPNum}";

            //String smsMessage = $"{HttpUtility.UrlEncode(selLangBLL.GetResourceValue("lbl_OTP", langId))}{OTPNum}";
            String smsMessage = $"Ung dung xin thong bao ma OTP cua ban la {OTPNum}";
            String siteName = custInfo.SiteName;
            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", siteName }
            };

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            var result = await httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);
            log2File.Info($"{PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTPLog");
            if (result.StatusCode == System.Net.HttpStatusCode.OK && resultResp.status == 0)
            {
                apiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {
                log2File.Info($"{PhoneNo},{custInfo.UserName},{OTPNum},status:{resultResp.status},reason:{resultResp.reason}", $"{siteName}_OTPErrorLog");
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


            return apiResult;
        }
    }
}
