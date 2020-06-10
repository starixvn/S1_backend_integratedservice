using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class SendGridBLL
    {
        private readonly SendGridConfig sendGridConfig;
        private readonly HttpHelper httpHelper;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Dictionary<string, string> headerParams;

        public SendGridBLL(IOptions<SendGridConfig> sendGridConfig, HttpHelper httpHelper, CustInfoBLL custInfoBLL)
        {
            this.sendGridConfig = sendGridConfig.Value;
            this.httpHelper = httpHelper;
            this.custInfoBLL = custInfoBLL;
            headerParams = new Dictionary<string, string> {
                { "Authorization", $"Bearer {sendGridConfig.Value.ApiKey}"}
            };
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

        public class SendMailBody
        {
            public From from { get; set; }
            public List<Personalization> personalizations { get; set; }
            public List<Content> content { get; set; }
        }

        public class Content
        {
            public string type { get; set; }
            public string value { get; set; }
        }

        public class From
        {
            public string name { get; set; }
            public string email { get; set; }
        }

        public class Personalization
        {
            public List<From> to { get; set; }
            public string subject { get; set; }
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, string subject, string content)
        {
            ApiResult apiResult = new ApiResult();

            string url = sendGridConfig.ApiUrl;
            SendMailBody sendMailBody = new SendMailBody();
            sendMailBody.from = new From()
            {
                name = sendGridConfig.mailFrom.name,
                email = sendGridConfig.mailFrom.email
            };

            sendMailBody.personalizations = new List<Personalization>();
            Personalization personalization = new Personalization();
            sendMailBody.personalizations.Add(personalization);
            personalization.subject = subject;
            personalization.to = new List<From>();
            From from = new From();
            personalization.to.Add(from);
            from.email = custInfo.Email;
            from.name = custInfo.UserName;
            sendMailBody.content = new List<Content>();
            Content content1 = new Content();
            sendMailBody.content.Add(content1);
            content1.type = "text/html";
            content1.value = content;

            var result = await httpHelper.HttpPost1Async(url, headerParams, JsonConvert.SerializeObject(sendMailBody), "application/json");
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
