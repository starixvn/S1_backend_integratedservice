using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CommonService.Service.Model.SendGridResetPassword;

namespace CommonService.Service.BLL
{
    public class RestPasswordBLL
    {
        private readonly IOptions<Dictionary<string, SendGridConfig>> sendGridConfig;
        private readonly HttpHelper httpHelper;
        private readonly CustInfoBLL custInfoBLL;
        private readonly AuthBLL authBLL;

        public RestPasswordBLL(IOptions<Dictionary<string, SendGridConfig>> sendGridConfig, HttpHelper httpHelper, CustInfoBLL custInfoBLL, AuthBLL authBLL)
        {
            this.sendGridConfig = sendGridConfig;
            this.httpHelper = httpHelper;
            this.custInfoBLL = custInfoBLL;
            this.authBLL = authBLL;
        }
        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo)
        {
            ApiResult apiResult = new ApiResult();

            if (!sendGridConfig.Value.ContainsKey(custInfo.SiteName))
            {
                apiResult.ErrorCode = ErrorCode.Fail;
            }

            var config = sendGridConfig.Value[custInfo.SiteName];

            Dictionary<string, string> headerParams = new Dictionary<string, string> {
                { "Authorization", $"Bearer {config.ApiKey}"}
            };

            string url = config.ApiUrl;
            SendGridResetPasswordBody sendGridResetPasswordBody = new SendGridResetPasswordBody();
            sendGridResetPasswordBody.from = new NameEmail()
            {
                name = config.mailFrom.name,
                email = config.mailFrom.email
            };

            sendGridResetPasswordBody.personalizations = new List<ResetPasswordPersonalization>();

            ResetPasswordPersonalization resetPasswordPersonalization = new ResetPasswordPersonalization();
            sendGridResetPasswordBody.personalizations.Add(resetPasswordPersonalization);

            resetPasswordPersonalization.to = new List<NameEmail>();
            NameEmail to = new NameEmail();
            to.email = custInfo.Email;
            to.name = custInfo.UserName;
            resetPasswordPersonalization.to.Add(to);
            var key = authBLL.ApplyToken(custInfo);
            DynamicTemplateData dynamicTemplateData = new DynamicTemplateData();
            dynamicTemplateData.resetpwdurl = $"{config.ResetPasswordUrl}?key={key}";
            resetPasswordPersonalization.dynamic_template_data = dynamicTemplateData;

            sendGridResetPasswordBody.template_id = config.ResetPasswordTemplate[custInfo.UserLang];

            var result = await httpHelper.HttpPost1Async(url, headerParams, JsonConvert.SerializeObject(sendGridResetPasswordBody), "application/json");
            if (result.StatusCode == System.Net.HttpStatusCode.Accepted)
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
