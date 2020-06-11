using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class SendGridTempBLL
    {
        private readonly SendGridConfig sendGridConfig;
        private readonly HttpHelper httpHelper;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Dictionary<string, string> headerParams;

        public SendGridTempBLL(IOptions<SendGridConfig> sendGridConfig, HttpHelper httpHelper, CustInfoBLL custInfoBLL)
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
            public string template_id { get; set; }
        }

        public class From
        {
            public string name { get; set; }
            public string email { get; set; }
        }

        public class Personalization
        {
            public List<From> to { get; set; }
            public Dynamic_template_data dynamic_template_data { get; set; }
        }

        public class Dynamic_template_data
        {
            public string resetpwdurl { get; set; }
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, string template_id)
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

            personalization.to = new List<From>();
            From from = new From();
            //from.email = custInfo.Email;
            //from.name = custInfo.UserName;
            from.email = "jeff.wu@ailabpte.co";
            from.name = "哈哈哈哈哈";
            personalization.to.Add(from);

            Dynamic_template_data dynamic_Template_Data = new Dynamic_template_data();
            //----will dynamic change Url
            dynamic_Template_Data.resetpwdurl = "https://www.google.com";
            //----will dynamic change Url
            personalization.dynamic_template_data = dynamic_Template_Data;

            sendMailBody.template_id = template_id;

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
