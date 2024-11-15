using CommonService.Service.Model.enums;
using CommonService.Service.Model;

namespace CommonService.Service.BLL
{
    public class WelcomeEmailBLL
    {
        private readonly TemplateType template = TemplateType.WelcomeMail;
        private readonly Dictionary<int, MailStatusSetting> _mailSettingConfig;
        private readonly CustInfoBLL _custInfoBll;

        private readonly SendGridBLL _sendGridBll;
        private readonly IBMBLL _iBMBll;

        public WelcomeEmailBLL(IServiceProvider provider)
        {
            _mailSettingConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MailStatusSetting>("StatusSetting");
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();

            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _iBMBll = provider.GetRequiredService<IBMBLL>();
        }

        public async Task<ApiResult> SendMailAsync(int custId)
        {
            var apiResult = new ApiResult();
            var custInfo = _custInfoBll.GetCustInfoProfile(custId);

            if (!ParameterCheck(custInfo))
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
                return apiResult;
            }

            if (!_mailSettingConfig.TryGetValue(custInfo.SiteId, out var config))
            {
                return new ApiResult
                {
                    ErrorCode = ErrorCode.SettingNotFound,
                    Message = "MailSettingConfig not found."
                };
            }

            switch (config.Mode)
            {
                case (int)MailType.SendGrid:
                    return await _sendGridBll.SendMailAsync(custInfo, null, template);
                case (int)MailType.IBM:
                    return await _iBMBll.SendMailAsync(custInfo, null, template);
                default:
                    return new ApiResult
                    {
                        ErrorCode = ErrorCode.SettingNotFound,
                        Message = "MailType not define."
                    };
            }
        }

        private bool ParameterCheck(CustInfoProfile custInfo)
        {
            if (string.IsNullOrWhiteSpace(custInfo?.UserName)
                || string.IsNullOrWhiteSpace(custInfo?.Email)
                || string.IsNullOrWhiteSpace(custInfo?.IsoCode))
                return false;

            return true;
        }
    }
}
