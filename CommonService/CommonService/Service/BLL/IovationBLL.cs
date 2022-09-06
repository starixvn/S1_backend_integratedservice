using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using CommonService.Service.DAL;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class IovationBLL
    {
        private readonly IOptions<IovationServicesConfig> iovationServicesConfig;
        private readonly IovationService iovationService;
        private readonly HttpHelper httpHelper;

        public IovationBLL(IOptions<IovationServicesConfig> iovationServicesConfig, IovationService iovationService, HttpHelper httpHelper)
        {
            this.iovationServicesConfig = iovationServicesConfig;
            this.iovationService = iovationService;
            this.httpHelper = httpHelper;
        }
        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            var apiResult = new ApiResult<IovationCheckResult>();

            Dictionary<string, dynamic> sendIovationChecks = new Dictionary<string, dynamic>
            {
                { "accountCode", iovationChecksRequest.accountCode },
                { "blackbox", iovationChecksRequest.blackbox },
                { "statedIp", iovationChecksRequest.statedIp },
                { "transactionInsight",iovationChecksRequest.transactionInsight},
                { "type",iovationChecksRequest.type}
            };

            string Authtoken = $"{iovationServicesConfig.Value.SubscriberID}/{iovationServicesConfig.Value.SubscriberAccount}:{iovationServicesConfig.Value.Password}";
            byte[] bytes = System.Text.Encoding.GetEncoding("utf-8").GetBytes(Authtoken);
            //編成 Base64 字串
            string Base64Authtoken = Convert.ToBase64String(bytes);

            Dictionary<string, string> Authorization = new Dictionary<string, string>
            {
                { "Authorization", $"Basic {Base64Authtoken}" }
            };

            var parameters = JsonConvert.SerializeObject(sendIovationChecks);
            using var result = await httpHelper.HttpPost1Async(iovationServicesConfig.Value.ResourceURL, Authorization, parameters, "application/json");
            var resultResp = JsonConvert.DeserializeObject<IovationResult>(await result.Content.ReadAsStringAsync());
            var resultIns = iovationService.IovationResultIns(resultResp);

            if (result.StatusCode == System.Net.HttpStatusCode.OK && resultIns > 0)
            {
                var deviceId = resultResp?.details?.device?.alias;
                var deviceType = resultResp?.details?.device?.type;
                var score = resultResp?.details?.ruleResults?.score;

                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.result;
                apiResult.Data = new IovationCheckResult()
                {
                    Result = resultResp.result,
                    Reason = resultResp.reason,
                    DeviceId = string.IsNullOrEmpty(deviceId) ? "" : deviceId,
                    DeviceType = string.IsNullOrEmpty(deviceType) ? "" : deviceType ,
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
