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

        public MoviderSMSBLL(IOptions<Dictionary<string, MoviderVerifySMSConfig>> moviderVerifySMSConfig, HttpHelper httpHelper)
        {
            this.moviderVerifySMSConfig = moviderVerifySMSConfig;
            this.httpHelper = httpHelper;
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
                { "from", config.From },
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
        public async Task<ApiResult> AcknowledgeVerifySMSAsync(CustInfoProfile custInfo, string request_id, string code)
        {
            ApiResult apiResult = new ApiResult();
            var config = moviderVerifySMSConfig.Value[custInfo.SiteName];
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
                apiResult.ErrorCode = 0;
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
