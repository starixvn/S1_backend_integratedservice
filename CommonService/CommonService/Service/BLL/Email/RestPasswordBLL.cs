using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.BLL
{
    public class RestPasswordBLL
    {
        private readonly TemplateType template = TemplateType.ResetPassword;
        private readonly AuthBLL _authBLL;

        private readonly Dictionary<int, MailStatusSetting> _mailSettingConfig;
        private readonly CustInfoBLL _custInfoBll;

        private readonly SendGridBLL _sendGridBll;
        private readonly IBMBLL _iBMBll;

        public RestPasswordBLL(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _mailSettingConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MailStatusSetting>("StatusSetting");
            _custInfoBll = provider.GetRequiredService<CustInfoBLL>();

            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
            _iBMBll = provider.GetRequiredService<IBMBLL>();
        }

        public async Task<ApiResult> SendMailAsync(int custId, string url)
        {
            var apiResult = new ApiResult();

            var custInfo = _custInfoBll.GetCustInfoProfile(custId);

            if (!_mailSettingConfig.TryGetValue(custInfo.SiteId, out var config))
            {
                return new ApiResult<SendOTPEmailResult>
                {
                    ErrorCode = ErrorCode.SettingNotFound,
                    Message = "MailSettingConfig not found."
                };
            }

            if (!ParameterCheck(custInfo))
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
                return apiResult;
            }

            var resetPasswordUrl = GenResetPasswordUrl(custInfo, url, config);

            return await SendMailAsync(config, custInfo, resetPasswordUrl);
        }

        private bool ParameterCheck(CustInfoProfile custInfo)
        {
            if (string.IsNullOrWhiteSpace(custInfo?.UserName)
                || string.IsNullOrWhiteSpace(custInfo?.Email))
                return false;

            return true;
        }

        private string GenResetPasswordUrl(CustInfoProfile custInfo, string url, MailStatusSetting config)
        {
            var key = _authBLL.ApplyToken(custInfo);
            url ??= config.ResetPasswordUrl;
            return $"{url}?key={key}";
        }

        private async Task<ApiResult> SendMailAsync(MailStatusSetting config, CustInfoProfile custInfo, string resetPasswordUrl)
        {
            switch (config.Mode)
            {
                case (int)MailType.SendGrid:
                    return await _sendGridBll.SendMailAsync(custInfo, new { resetpwdurl = resetPasswordUrl }, template);
                case (int)MailType.IBM:
                    return await _iBMBll.SendMailAsync(custInfo, new { resetpwdurl = resetPasswordUrl }, template);
                default:
                    return new ApiResult
                    {
                        ErrorCode = ErrorCode.SettingNotFound,
                        Message = "MailType not define."
                    };
            }
        }
    }
}
