using System.Collections.Generic;

namespace CommonService.Service.Model
{
    public class SendGridResetPassword
    {
        public class SendGridResetPasswordBody : SendMailBody
        {
            public List<ResetPasswordPersonalization> personalizations { get; set; }
            public string template_id { get; set; }
        }

        public class ResetPasswordPersonalization : Personalization
        {
            public DynamicTemplateData dynamic_template_data { get; set; }
        }
        public class DynamicTemplateData
        {
            public string subject { get; set; }
            public string resetpwdurl { get; set; }

        }
    }
}
