using System.Net;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class SendGridBLL
    {
        private readonly Dictionary<int, SendGridConfig> _sendGridConfig;
        private readonly HttpHelper _httpHelper;

        public SendGridBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _sendGridConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SendGridConfig>("SendGrid");
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

            if (!_sendGridConfig.TryGetValue(custInfo.SiteId, out var config))
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.Fail
                };
            }

            var headerParams = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {config.ApiKey}"}
            };

            var apiUrl = config.ApiUrl;
            var (verify, template) = CreateTemplate(type, config, GetUserLang(custInfo.UserLang));
            var sendGridResetPasswordBody = new SendGridTemplateBody
            {
                from = new NameEmail { name = config.mailFrom.name, email = config.mailFrom.email },
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
                template_id = template
            };

            if (!verify)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = template;
                return apiResult;
            }

            using var result = await _httpHelper.HttpPost1Async(apiUrl, headerParams, JsonConvert.SerializeObject(sendGridResetPasswordBody), "application/json");
            if (result.StatusCode == HttpStatusCode.Accepted)
            {
                apiResult.ErrorCode = ErrorCode.Success;
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

        private (bool, string) CreateTemplate(TemplateType type, SendGridConfig config, string userLang)
        {
            var lang = "en";
            if (config.ResetPasswordTemplate.ContainsKey(userLang) && config.VerifyMailTemplate.ContainsKey(userLang))
                lang = userLang;

            return type switch
            {
                TemplateType.ResetPassword => config.ResetPasswordTemplate.TryGetValue(lang, out var resetPasswordValue)
                    ? (true, resetPasswordValue)
                    : (false, $"ResetPassword template lang key ({lang}) not define"),
                TemplateType.VerificationMail => config.VerifyMailTemplate.TryGetValue(lang, out var verificationMailValue)
                    ? (true, verificationMailValue)
                    : (false, $"VerificationMail template lang key ({lang}) not define"),
                TemplateType.WelcomeMail => config.WelcomeMailTemplate.TryGetValue(lang, out var welcomeMailValue)
                    ? (true, welcomeMailValue)
                    : (false, $"WelcomeMail template lang key ({lang}) not define"),
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
