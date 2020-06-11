namespace CommonService.Service.Model
{
    public class SendGridConfig
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public MailFrom mailFrom { get; set; }
    }
    public class MailFrom
    {
        public string name { get; set; }
        public string email { get; set; }
    }
}
