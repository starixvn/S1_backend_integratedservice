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

            string transactionInsight = JsonConvert.SerializeObject(iovationChecksRequest.transactionInsight);
            Dictionary<string, string> sendIovationChecks = new Dictionary<string, string>
            {
                { "accountCode", iovationChecksRequest.accountCode },
                { "blackbox", iovationChecksRequest.blackbox },
                { "statedIp", iovationChecksRequest.statedIp },
                { "transactionInsight",transactionInsight},
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

            var result = await httpHelper.HttpFromPostAsync(iovationServicesConfig.Value.ResourceURL, Authorization, sendIovationChecks, "application/json");
            var resultResp = JsonConvert.DeserializeObject<IovationResult>(result.Content.ReadAsStringAsync().Result);
            var resultIns = iovationService.IovationResultIns(resultResp);

            //if (result.StatusCode == System.Net.HttpStatusCode.OK && resultIns > 0  )
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
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
