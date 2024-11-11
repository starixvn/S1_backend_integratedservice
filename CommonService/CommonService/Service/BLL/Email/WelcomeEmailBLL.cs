using CommonService.Service.Library;
using CommonService.Service.Model.enums;
using CommonService.Service.Model;

namespace CommonService.Service.BLL
{
    public class WelcomeEmailBLL
    {

        private readonly Dictionary<int, SendGridConfig> _sendGridConfig;
        private readonly HttpHelper _httpHelper;
        private readonly AuthBLL _authBLL;
        private readonly SendGridBLL _sendGridBll;
        private readonly TemplateType template = TemplateType.WelcomeMail;

        public WelcomeEmailBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _sendGridConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SendGridConfig>("SendGrid");
            _sendGridBll = provider.GetRequiredService<SendGridBLL>();
        }

        public async Task<ApiResult> SendMailAsync(CustInfoProfile custInfo, string resetPasswordUrl)
        {
            var apiResult = new ApiResult<SendOTPEmailResult>();

            var sendMailResult = await _sendGridBll.SendMailAsync(custInfo, null, template);

            if (sendMailResult.ErrorCode == ErrorCode.Success)
            {
                apiResult.ErrorCode = (ErrorCode)insertOtpResult.Status;
                apiResult.Data = new SendOTPEmailResult
                {
                    Status = insertOtpResult.Status,
                    StartUnixTime = insertOtpResult.StartUnixTime,
                    Request_id = insertOtpResult.Request_id
                };
            }
            else
            {
                apiResult.ErrorCode = sendMailResult.ErrorCode;
                apiResult.Message = sendMailResult.Message;
            }

            return apiResult;
        }
    }
}
