using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using CommonService.Service.DAL;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public async Task<ApiResult> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            var apiResult = new ApiResult();

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
            var result = await httpHelper.HttpPost1Async(iovationServicesConfig.Value.ResourceURL, Authorization, parameters, "application/json");
            var resultResp = JsonConvert.DeserializeObject<IovationResult>(result.Content.ReadAsStringAsync().Result);
            var resultIns = iovationService.IovationResultIns(resultResp);

            if (result.StatusCode == System.Net.HttpStatusCode.OK && resultIns > 0)
                //if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = resultResp.result;
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
