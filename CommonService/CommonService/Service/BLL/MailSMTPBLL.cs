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
        private readonly SelLangBLL _selLangBLL;
        private readonly AuthBLL _authBLL;

        public class EmailDetails
        {
            public string Subject { get; init; }
            public string Content { get; init; }
        }

        public MailSMTPBLL(IConfiguration configuration, IServiceProvider provider, SmtpClient smtpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _selLangBLL = provider.GetRequiredService<SelLangBLL>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _smtpClient = smtpClient;
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, string dynamicUrl, TemplateType type)
        {
            var emailDetails = MakeEmailContext(custInfo, dynamicUrl, type);
            var mailTo = new MailAddressCollection { new MailAddress(custInfo.Email) };
            return await PublishHtmlAsync(mailTo, new MailAddressCollection(), new MailAddressCollection(), emailDetails.Subject, emailDetails.Content);
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

        private EmailDetails MakeEmailContext(CustInfoProfile custInfo, string dynamicUrl, TemplateType type)
        {
            var userLang = custInfo.UserLang;
            var siteName = custInfo.SiteName;

            var mailContent = string.Empty;
            var mailSubject = string.Empty;

            if (type == TemplateType.VerificationMail)
            {
                mailSubject = string.Format(_selLangBLL.GetResourceValue("lbl_VerifyEmailSubject", userLang), siteName);
                mailContent = string.Format(_selLangBLL.GetResourceValue("lbl_VerifyEmailContent", userLang), dynamicUrl, siteName);
            } else if (type == TemplateType.ResetPassword)
            {
                var key = _authBLL.ApplyToken(custInfo);
                var resetPasswordUrl = $"{dynamicUrl}?key={key}";
                mailSubject = string.Format(_selLangBLL.GetResourceValue("lbl_ForgottenPasswordEmailSubject", userLang), siteName);
                mailContent = string.Format(_selLangBLL.GetResourceValue("lbl_ForgottenPasswordEmailContent", userLang), resetPasswordUrl, siteName);
            }
            return new EmailDetails { Subject = mailSubject, Content = mailContent};
        }

    }
}
