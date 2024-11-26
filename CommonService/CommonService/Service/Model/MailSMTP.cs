namespace CommonService.Service.Model
{
    public class EmailSettingsOptions
    {
        public string FromMail { get; set; } = default!;

        public string FromName { get; set; } = default!;

        public string SmtpServer { get; set; } = default!;

        public int Port { get; set; }

        public string Username { get; set; } = default!;

        public string Password { get; set; } = default!;
    }
}
