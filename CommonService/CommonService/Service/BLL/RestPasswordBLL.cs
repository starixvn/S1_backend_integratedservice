using System.Net;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static CommonService.Service.Model.SendGridResetPassword;

namespace CommonService.Service.BLL
{
    public class RestPasswordBLL
    {
        private readonly IOptions<Dictionary<string, SendGridConfig>> sendGridConfig;
        private readonly HttpHelper httpHelper;
        private readonly AuthBLL authBLL;

        public RestPasswordBLL(IOptions<Dictionary<string, SendGridConfig>> sendGridConfig, HttpHelper httpHelper, AuthBLL authBLL)
        {
            this.sendGridConfig = sendGridConfig;
            this.httpHelper = httpHelper;
            this.authBLL = authBLL;
        }
        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, string resetPasswordUrl)
        {
            var apiResult = new ApiResult();

            if (!sendGridConfig.Value.ContainsKey(custInfo.SiteName))
            {
                apiResult.ErrorCode = ErrorCode.Fail;
            }

            var config = sendGridConfig.Value[custInfo.SiteName];

            var headerParams = new Dictionary<string, string> {
                { "Authorization", $"Bearer {config.ApiKey}"}
            };

            var url = config.ApiUrl;
            var sendGridResetPasswordBody = new SendGridResetPasswordBody
            {
                from = new NameEmail
                {
                    name = config.mailFrom.name,
                    email = config.mailFrom.email
                },
                personalizations = new List<ResetPasswordPersonalization>()
            };

            var resetPasswordPersonalization = new ResetPasswordPersonalization();
            sendGridResetPasswordBody.personalizations.Add(resetPasswordPersonalization);

            resetPasswordPersonalization.to = new List<NameEmail>();
            var to = new NameEmail
            {
                email = custInfo.Email,
                name = custInfo.UserName
            };
            resetPasswordPersonalization.to.Add(to);
            var key = authBLL.ApplyToken(custInfo);
            var dynamicTemplateData = new DynamicTemplateData();
            resetPasswordUrl ??= config.ResetPasswordUrl;
            dynamicTemplateData.resetpwdurl = $"{resetPasswordUrl}?key={key}";
            resetPasswordPersonalization.dynamic_template_data = dynamicTemplateData;
            var lang = "en";
            if (config.ResetPasswordTemplate.ContainsKey(custInfo.UserLang))
                lang = custInfo.UserLang;
            sendGridResetPasswordBody.template_id = config.ResetPasswordTemplate[lang];

            using var result = await httpHelper.HttpPost1Async(url, headerParams, JsonConvert.SerializeObject(sendGridResetPasswordBody), "application/json");
            if (result.StatusCode == HttpStatusCode.Accepted)
            {
                apiResult.ErrorCode = 0;
            }
            else
            {
                var resultResp = JsonConvert.DeserializeObject<Result>(result.Content.ReadAsStringAsync().Result);
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.errors[0].message;
                apiResult.RawMessage = resultResp.errors[0].message;
            }
            return apiResult;
        }
    }
}
