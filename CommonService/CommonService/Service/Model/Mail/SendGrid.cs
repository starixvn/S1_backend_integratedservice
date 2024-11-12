namespace CommonService.Service.Model
{
    public class SendGridConfig
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public NameEmail mailFrom { get; set; }
        public Dictionary<string, string> ResetPasswordTemplate { get; set; }
        public Dictionary<string, string> VerifyMailTemplate { get; set; }
        public Dictionary<string, string> WelcomeMailTemplate { get; set; }
        public string ResetPasswordUrl { get; set; }
    }
    public class NameEmail
    {
        public string name { get; set; }
        public string email { get; set; }
    }

    public class SendMailBody
    {
        public NameEmail from { get; set; }
    }

    public class Personalization
    {
        public List<NameEmail> to { get; set; }
    }

    public class SendGridTemplateBody : SendMailBody
    {
        public List<TemplatePersonalization> personalizations { get; set; }
        public string template_id { get; set; }
    }

    public class TemplatePersonalization : Personalization
    {
        public dynamic dynamic_template_data { get; set; }
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

}
