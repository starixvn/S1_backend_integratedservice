using System.Collections.Generic;
using CommonService.Service.Model;

namespace CommonService.Service.Model
{
    public class SendGridConfig
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public NameEmail mailFrom { get; set; }
        public string ResetPasswordTemplateId { get; set; }
        public string ResetPasswordUrl { get; set; }
    }
    public class NameEmail
    {
        public string name { get; set; }
        public string email { get; set; }
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
        public NameEmail from { get; set; }
    }
    public class SendCustomContentMailBody : SendMailBody
    {
        public List<Content> content { get; set; }
        public List<CustomPersonalization> personalizations { get; set; }
    }

    public class Content
    {
        public string type { get; set; }
        public string value { get; set; }
    }
    public class Personalization
    {
        public List<NameEmail> to { get; set; }
    }
    public class CustomPersonalization : Personalization
    {
        public string subject { get; set; }
    }

}
