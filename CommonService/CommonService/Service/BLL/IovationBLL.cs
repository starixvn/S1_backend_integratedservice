using System.Net;
using System.Text;

using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class IovationBLL
    {
        private readonly IovationServicesConfig _iovationServicesConfig;
        private readonly IovationService iovationService;
        private readonly HttpHelper httpHelper;

        public IovationBLL(IServiceProvider provider)
        {
            httpHelper = provider.GetRequiredService<HttpHelper>();
            iovationService = provider.GetRequiredService<IovationService>();
            var configBLL = provider.GetRequiredService<ConfigBLL>();
            _iovationServicesConfig = configBLL.GetConfigValue<IovationServicesConfig>("Iovation");
        }

        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            var apiResult = new ApiResult<IovationCheckResult>();

            var sendIovationChecks = new Dictionary<string, dynamic>
            {
                { "accountCode", iovationChecksRequest.accountCode },
                { "blackbox", iovationChecksRequest.blackbox },
                { "statedIp", iovationChecksRequest.statedIp },
                { "transactionInsight",iovationChecksRequest.transactionInsight},
                { "type",iovationChecksRequest.type}
            };

            var Authtoken = $"{_iovationServicesConfig.SubscriberID}/{_iovationServicesConfig.SubscriberAccount}:{_iovationServicesConfig.Password}";
            var bytes = Encoding.GetEncoding("utf-8").GetBytes(Authtoken);
            //編成 Base64 字串
            var Base64Authtoken = Convert.ToBase64String(bytes);

            var Authorization = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {Base64Authtoken}" }
            };

            var parameters = JsonConvert.SerializeObject(sendIovationChecks);
            using var result = await httpHelper.HttpPost1Async(_iovationServicesConfig.ResourceURL, Authorization, parameters, "application/json");
            var resultResp = JsonConvert.DeserializeObject<IovationResult>(await result.Content.ReadAsStringAsync());
            var resultIns = iovationService.IovationResultIns(resultResp);

            if (result.StatusCode == HttpStatusCode.OK && resultIns > 0)
            {
                var deviceId = resultResp?.details?.device?.alias;
                var deviceType = resultResp?.details?.device?.type;
                var score = resultResp?.details?.ruleResults?.score;

                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.result;
                apiResult.Data = new IovationCheckResult
                {
                    Result = resultResp.result,
                    Reason = string.IsNullOrEmpty(resultResp.reason) ? "" : resultResp.reason,
                    DeviceId = string.IsNullOrEmpty(deviceId) ? "" : deviceId,
                    DeviceType = string.IsNullOrEmpty(deviceType) ? "" : deviceType,
                    Score = score ?? 0
                };
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
            }

            return apiResult;
        }
    }
}
