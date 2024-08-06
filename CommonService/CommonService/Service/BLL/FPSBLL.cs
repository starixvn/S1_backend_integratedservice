using System.Net;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class FPSBLL
    {
        private readonly Dictionary<int, FPSServicesConfig> _fpsServicesConfig;
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<FPSBLL> _logger;

        public FPSBLL(IServiceProvider provider)
        {
            
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetRequiredService<ILogger<FPSBLL>>();
            _fpsServicesConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<FPSServicesConfig>("FPS");
        }

        public async Task<ApiResult> FPSChecksAsync(CustInfoProfile custInfo, FPSChecksRequest fpsChecksRequest)
        {
            var apiResult = new ApiResult();
            var config = _fpsServicesConfig[custInfo.SiteId];
            var url = config.ResourceURL;
            
            var sendIovationChecks = new Dictionary<string, string>
            {
                { "Account", custInfo.TpUserName },
                { "Subscriber",config.Subscriber},
                { "SubscriberDate",new DateTimeOffset(DateTime.Now).ToOffset(TimeSpan.FromHours(-4)).ToString()},
                { "DI",fpsChecksRequest.DI},
                { "BI",fpsChecksRequest.BI},
                { "Ip",fpsChecksRequest.IP},
                { "UserAgent",fpsChecksRequest.UserAgent},
                { "DetecasAnalysis", fpsChecksRequest.DetecasAnalysis },
                { "Action", "login"},
                { "ActionResult", "Successfully" },
                { "AcceptsTypes", fpsChecksRequest.AcceptsTypes },
                { "SessionKey", fpsChecksRequest.SessionKey },
                { "Token", fpsChecksRequest.Token },
                { "Url",fpsChecksRequest.Url}
            };
            
            _logger.LogInformation($"FPSCheck request:{JsonConvert.SerializeObject(sendIovationChecks)}");
            
            using var result = await _httpHelper.HttpPost1Async(url, null, JsonConvert.SerializeObject(sendIovationChecks), "application/json");
            
            _logger.LogInformation($"FPSCheck result:{JsonConvert.SerializeObject(result)}");
            
            if (result.StatusCode == HttpStatusCode.OK)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = result.Content.ReadAsStringAsync().Result;
            }

            return apiResult;
        }
    }
}
