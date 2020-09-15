using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class TwilioSMSBLL
    {
        private readonly IOptions<Dictionary<string, TwilioVerifySMSServices>> twilioVerifySMSServices;
        private readonly HttpHelper httpHelper;

        public TwilioSMSBLL(IOptions<Dictionary<string, TwilioVerifySMSServices>> twilioVerifySMSServices, HttpHelper httpHelper)
        {
            this.twilioVerifySMSServices = twilioVerifySMSServices;
            this.httpHelper = httpHelper;
        }
        public async Task<ApiResult> SendVerifySMSAsync(CustInfoProfile custInfo, string phone, string ip)
        {
            var apiResult = new ApiResult();
            var config = twilioVerifySMSServices.Value[custInfo.SiteName];
            string url = $"{config.VerificationService}{config.ServiceID}/{config.Verifications}";

            Dictionary<string, string> langlist = new Dictionary<string, string>()
            {
                { "en", "en" },
                { "th", "th" },
                { "vn", "vi" },
                { "id", "id" },
                { "cs", "zh" },
                { "ch", "zh-CN" },
            };

            if (!langlist.TryGetValue(custInfo.UserLang, out string langvalue))
            {
                langvalue = "en";
            }

            Dictionary<string, string> RateLimits = new Dictionary<string, string>
            {
                { config.RateLimits, $"{phone} {ip}" },
            };
            string _RateLimits = JsonConvert.SerializeObject(RateLimits);
            Dictionary<string, string> sendVerifySMS = new Dictionary<string, string>
            {
                { "To", phone },
                { "Channel", "sms" },
                { "Locale", langvalue },
                { "RateLimits",_RateLimits}
            };

            string Authtoken = $"{config.AccountSID}:{config.Authtoken}";
            byte[] bytes = System.Text.Encoding.GetEncoding("utf-8").GetBytes(Authtoken);
            //編成 Base64 字串
            string Base64Authtoken = Convert.ToBase64String(bytes);

            Dictionary<string, string> Authorization = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {Base64Authtoken}" }
            };

            var result = await httpHelper.HttpFromPostAsync(url, Authorization, sendVerifySMS, "application/x-www-form-urlencoded");
            var resultResp = JsonConvert.DeserializeObject<TwilioVerificationsResult>(result.Content.ReadAsStringAsync().Result);
            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.sid;
            }
            else if (resultResp.code == 20429)
            {
                apiResult.ErrorCode = ErrorCode.Too_Many_Requests;
                apiResult.Message = resultResp.message;
                apiResult.RawErrorCode = Convert.ToString(resultResp.code);
                apiResult.RawMessage = Convert.ToString(resultResp.status);
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
                apiResult.RawErrorCode = Convert.ToString(resultResp.code);
                apiResult.RawMessage = Convert.ToString(resultResp.status);
            }

            return apiResult;
        }
        public async Task<ApiResult> VerificationCheckSMSAsync(string siteName, string sid, string code)
        {
            ApiResult apiResult = new ApiResult();
            var config = twilioVerifySMSServices.Value[siteName];
            string url = $"{config.VerificationService}{config.ServiceID}/{config.VerificationChecks}";

            Dictionary<string, string> VerificationCheckSMS = new Dictionary<string, string>
            {
                { "VerificationSid", sid },
                { "Code", code }
            };

            string Authtoken = $"{config.AccountSID}:{config.Authtoken}";
            byte[] bytes = System.Text.Encoding.GetEncoding("utf-8").GetBytes(Authtoken);
            //編成 Base64 字串
            string Base64Authtoken = Convert.ToBase64String(bytes);

            Dictionary<string, string> Authorization = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {Base64Authtoken}" }
            };

            var result = await httpHelper.HttpFromPostAsync(url, Authorization, VerificationCheckSMS, "application/x-www-form-urlencoded");
            var resultResp = JsonConvert.DeserializeObject<TwilioVerificationsResult>(result.Content.ReadAsStringAsync().Result);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (resultResp.valid == "true")
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Message = resultResp.status;
                }
                else {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Message = resultResp.message;
                    apiResult.RawErrorCode = Convert.ToString(resultResp.code);
                    apiResult.RawMessage = Convert.ToString(resultResp.status);
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
                apiResult.RawErrorCode = Convert.ToString(resultResp.code);
                apiResult.RawMessage = Convert.ToString(resultResp.status);
            }

            return apiResult;
        }
    }
}
