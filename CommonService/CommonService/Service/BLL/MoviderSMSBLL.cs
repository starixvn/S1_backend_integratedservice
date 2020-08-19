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
            ApiResult apiResult = new ApiResult();
            var config = moviderVerifySMSConfig.Value[custInfo.SiteName];
            string url = config.SendVerifyUrl;

            Dictionary<string, string> langlist = new Dictionary<string, string>();
            langlist.Add("en", "en-us");
            langlist.Add("th", "th-th");
            langlist.Add("vn", "vi-vn");

            if (!langlist.TryGetValue(custInfo.UserLang, out string langvalue))
            {
                langvalue = "en-us";
            }

            Dictionary<string, string> sendVerifySMS = new Dictionary<string, string>();
            sendVerifySMS.Add("api_key", config.ApiKey);
            sendVerifySMS.Add("api_secret", config.ApiSecret);
            sendVerifySMS.Add("code_length", config.CodeLength);
            sendVerifySMS.Add("from", config.From);
            sendVerifySMS.Add("language", langvalue);
            sendVerifySMS.Add("next_event_wait", config.NextEventWait);
            sendVerifySMS.Add("pin_expire", config.PinExpire);
            sendVerifySMS.Add("to", phone);

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

            Dictionary<string, string> AcknowledgeVerifySMS = new Dictionary<string, string>();
            AcknowledgeVerifySMS.Add("api_key", config.ApiKey);
            AcknowledgeVerifySMS.Add("api_secret", config.ApiSecret);
            AcknowledgeVerifySMS.Add("request_id", request_id);
            AcknowledgeVerifySMS.Add("code", code);

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
