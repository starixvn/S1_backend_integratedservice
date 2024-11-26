using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class MailSMTPBLL
    {
        private readonly IConfiguration _configuration;
        private readonly SmtpClient _smtpClient;

        public MailSMTPBLL(IConfiguration configuration, SmtpClient smtpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _smtpClient = smtpClient;
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, dynamic dynamicTemplateData, TemplateType type)
        {
            string subject = "Zephyr Test SMTP"; 
            string verifyEmailUrl = dynamicTemplateData.verifyemailurl;

            var to = new MailAddressCollection { new MailAddress(custInfo.Email) };

            return await PublishHtmlAsync(to, new MailAddressCollection(), new MailAddressCollection(), subject, verifyEmailUrl);
        }

        private async Task<ApiResult> PublishHtmlAsync(
            MailAddressCollection to,
            MailAddressCollection cc,
            MailAddressCollection bcc,
            string subject,
            string content)
        {
            var emailSettings = _configuration.GetSection("EmailSetting").Get<EmailSettingsOptions>();

            if (!to.Any())
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.Fail
                };
            }

            var message = new MailMessage
            {
                From = new MailAddress(emailSettings.FromMail, emailSettings.FromName),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            foreach (var address in to)
            {
                message.To.Add(address);
            }

            foreach (var address in cc)
            {
                message.CC.Add(address);
            }

            foreach (var address in bcc)
            {
                message.Bcc.Add(address);
            }

            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(content, Encoding.UTF8, MediaTypeNames.Text.Html));

            try
            {
                await _smtpClient.SendMailAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.Fail,
                };
            } 
            finally
            {
                message.Dispose();
            }

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success
            };
        }
    }
}
