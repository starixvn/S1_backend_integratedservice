using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class MoviderSMSBLL
    {
        private readonly IOptions<Dictionary<string, MoviderVerifySMSConfig>> moviderVerifySMSConfig;
        private readonly HttpHelper httpHelper;
        private readonly OTPSMSService sOTPSMSService;

        public MoviderSMSBLL(IOptions<Dictionary<string, MoviderVerifySMSConfig>> moviderVerifySMSConfig, HttpHelper httpHelper, OTPSMSService sOTPSMSService)
        {
            this.moviderVerifySMSConfig = moviderVerifySMSConfig;
            this.httpHelper = httpHelper;
            this.sOTPSMSService = sOTPSMSService;
        }
        public async Task<ApiResult<SendOTPSMSResult>> SendMoviderVerifySMSAsync(CustInfoProfile custInfo, string phone, int custId)
        {
            var apiResult = new ApiResult<SendOTPSMSResult>();
            var Result = await SendVerifySMSAsync(custInfo, phone);
            if (Result.ErrorCode == ErrorCode.Success)
            {
                SendOTPSMSResult sOresult = sOTPSMSService.OTPVerifyinfoInsWithRequest_id(custId, Result.Message);
                apiResult.ErrorCode = (ErrorCode)sOresult.Status;
                apiResult.Data = sOresult;
                apiResult.Message = Result.Message;

            }
            else
            {
                apiResult.ErrorCode = Result.ErrorCode;
                apiResult.Message = Result.Message;
            }
            return apiResult;

        }
        public async Task<ApiResult> SendVerifySMSAsync(CustInfoProfile custInfo, string phone)
        {
            var apiResult = new ApiResult();
            var config = moviderVerifySMSConfig.Value[custInfo.SiteName];
            string url = config.SendVerifyUrl;

            Dictionary<string, string> langlist = new Dictionary<string, string>()
            {
                { "en", "en-us" },
                { "th", "th-th" },
                { "vn", "vi-vn" }
            };

            if (!langlist.TryGetValue(custInfo.UserLang, out string langvalue))
            {
                langvalue = "en-us";
            }

            Dictionary<string, string> sendVerifySMS = new Dictionary<string, string>
            {
                { "api_key", config.ApiKey },
                { "api_secret", config.ApiSecret },
                { "code_length", config.CodeLength },
                { "from", custInfo.SiteName },
                { "language", langvalue },
                { "next_event_wait", config.NextEventWait },
                { "pin_expire", config.PinExpire },
                { "to", phone }
            };

            var result = await httpHelper.HttpFromPostAsync(url, null, sendVerifySMS, "application/x-www-form-urlencoded");
            var resultResp = JsonConvert.DeserializeObject<MoviderResult>(result.Content.ReadAsStringAsync().Result);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = resultResp.request_id;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.error.description;
                apiResult.RawErrorCode = resultResp.error.code.ToString();
                apiResult.RawMessage = resultResp.error.name;
            }

            return apiResult;
        }
        public async Task<ApiResult> AcknowledgeVerifySMSAsync(string siteName, string request_id, string code)
        {
            ApiResult apiResult = new ApiResult();
            var config = moviderVerifySMSConfig.Value[siteName];
            string url = config.AcknowledgeVerifyUrl;

            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>
            {
                { "api_key", config.ApiKey },
                { "api_secret", config.ApiSecret },
                { "request_id", request_id },
                { "code", code }
            };

            var result = await httpHelper.HttpFromPostAsync(url, null, AcknowledgeVerifySMS, "application/x-www-form-urlencoded");
            var resultResp = JsonConvert.DeserializeObject<MoviderResult>(result.Content.ReadAsStringAsync().Result);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.price;
                apiResult.RawMessage = resultResp.request_id;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.error.description;
                apiResult.RawErrorCode = resultResp.error.code.ToString();
                apiResult.RawMessage = resultResp.error.name;
            }

            return apiResult;
        }
    }
}
