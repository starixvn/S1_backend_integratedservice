using System.Net;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class SendGridBLL
    {
        private readonly IOptions<Dictionary<string, SendGridConfig>> _sendGridConfig;
        private readonly HttpHelper _httpHelper;

        public SendGridBLL(IOptions<Dictionary<string, SendGridConfig>> sendGridConfig, HttpHelper httpHelper)
        {
            _sendGridConfig = sendGridConfig;
            _httpHelper = httpHelper;
        }

        public class Result
        {
            public List<Error> errors { get; set; }
        }

        public class Error
        {
            public string message { get; set; }
            public string field { get; set; }
            public string help { get; set; }
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, dynamic dynamicTemplateData, TemplateType type)
        {
            var apiResult = new ApiResult();

            if (!_sendGridConfig.Value.ContainsKey(custInfo.SiteName))
            {
                apiResult.ErrorCode = ErrorCode.Fail;
            }

            var config = _sendGridConfig.Value[custInfo.SiteName];

            var headerParams = new Dictionary<string, string> {
                { "Authorization", $"Bearer {config.ApiKey}"}
            };

            var apiUrl = config.ApiUrl;
            var sendGridResetPasswordBody = new SendGridTemplateBody
            {
                from = new NameEmail {name = config.mailFrom.name, email = config.mailFrom.email},
                personalizations = new List<TemplatePersonalization>
                {
                    new TemplatePersonalization
                    {
                        to = new List<NameEmail>
                        {
                            new NameEmail {name = custInfo.UserName, email = custInfo.Email}
                        },
                        dynamic_template_data = dynamicTemplateData
                    }
                },
                template_id = CreateTemplate(type, config, GetUserLang(custInfo.UserLang))
            };
            
            using var result = await _httpHelper.HttpPost1Async(apiUrl, headerParams, JsonConvert.SerializeObject(sendGridResetPasswordBody), "application/json");
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

        private string CreateTemplate(TemplateType type, SendGridConfig config, string userLang)
        {
            var lang = "en";
            if (config.ResetPasswordTemplate.ContainsKey(userLang) && config.VerifyMailTemplate.ContainsKey(userLang))
                lang = userLang;

            return type switch
            {
                TemplateType.ResetPassword => config.ResetPasswordTemplate[lang],
                TemplateType.VerificationMail => config.VerifyMailTemplate[lang],
                TemplateType.WelcomeMail => config.WelcomeMailTemplate[lang],
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, @"template type not define")
            };
        }

        private string GetUserLang(string langId)
        {
            var lang = langId.Split('_');
            return lang.Length > 1 ? lang[1] : lang[0];
        }
    }
}
