using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        private readonly IOptions<OTPSMSServiceConfig> OTPSMSServiceConfig;
        private readonly Tools tools;
        private readonly SelLangBLL selLangBLL;

        public OTPSMSBLL(HttpHelper httpHelper, OTPSMSService sOTPSMSService, IOptions<OTPSMSServiceConfig> OTPSMSServiceConfig,Tools tools, SelLangBLL selLangBLL)
        {
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
            this.OTPSMSServiceConfig = OTPSMSServiceConfig;
            this.tools = tools;
            this.selLangBLL = selLangBLL;



        }


        public async Task<ApiResult<SendOTPSMSResult>> GetOTPVerifyInfoAsync(int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.GetOTPVerifyinfo(custId);
            apiResult.ErrorCode = (ErrorCode)(result.Status == 1 ? 0 : 1000);
            apiResult.Data = result;


            return apiResult;
        }
        public async Task<ApiResult<SendOTPSMSResult>> OTPVerifyinfoInsAsync(int custId, string sOTPNum)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            SendOTPSMSResult result = sOTPSMSService.OTPVerifyinfoIns(custId, sOTPNum);
            apiResult.ErrorCode = (ErrorCode)result.Status;
            apiResult.Data = result;


            return apiResult;
        }

        public async Task<ApiResult> VerifyOPTAsync(VerifyOTPReq verifyOTPReq)
        {
            var apiResult = new ApiResult();
            int? result = sOTPSMSService.VerifyOPT(verifyOTPReq);
            apiResult.ErrorCode = (ErrorCode)(result == 0 ? 0 : 1000);
            return apiResult;
        }
        public string MakeSignString(string account, string pwd, string datatime)
        {
            string tmpstr = $"{account}{pwd}{datatime}";
           return tools.ToMD5(tmpstr);
            
        }
       
        public async Task<ApiResult> SendOTPSMSAsync(string PhoneNo, string OTPNum, string langId)
        {
            var apiResult = new ApiResult();
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string sign = MakeSignString(OTPSMSServiceConfig.Value.Account, OTPSMSServiceConfig.Value.Password, dateTime);
            string url = $"{OTPSMSServiceConfig.Value.ResourceURL}account={OTPSMSServiceConfig.Value.Account}&sign={sign}&datetime={dateTime}";

            //String smsMessage = $"{selLangBLL.GetResourceValue("lbl_OTP", langId)} {OTPNum}";
            
            String smsMessage = $"{HttpUtility.UrlEncode(selLangBLL.GetResourceValue("lbl_OTP", langId))}{OTPNum}";
            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "content", smsMessage },
                { "numbers", PhoneNo},
                { "senderid", "" }
            };

            var parameters = JsonConvert.SerializeObject(AcknowledgeVerifySMS);
            var result = await httpHelper.HttpPost1Async(url, null, parameters, "application/json");
            var resultResp = JsonConvert.DeserializeObject<SKYLineSMSResult>(result.Content.ReadAsStringAsync().Result);

            if (result.StatusCode == System.Net.HttpStatusCode.OK && resultResp.status == 0)
            {
                    apiResult.ErrorCode = ErrorCode.Success;
            }
            else
            {

                if (resultResp.status == -6 || resultResp.status == -7 || resultResp.status == -8 || resultResp.status == -9)//號碼錯誤傳5002給前端
                {
                    apiResult.ErrorCode = ErrorCode.PhoneError;
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                   // apiResult.Message = $"StatusCode = {resultResp.status} Message = {resultResp.reason}";
                }

            }


            return apiResult;
        }
    }
}
