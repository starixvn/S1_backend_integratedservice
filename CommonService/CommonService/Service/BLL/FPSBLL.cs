using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class FPSBLL
    {
        private readonly IOptions<Dictionary<string, FPSServicesConfig>> fpsServicesConfig;
        private readonly HttpHelper httpHelper;

        public FPSBLL(IOptions<Dictionary<string, FPSServicesConfig>> fpsServicesConfig, HttpHelper httpHelper)
        {
            this.fpsServicesConfig = fpsServicesConfig;
            this.httpHelper = httpHelper;
        }
        public async Task<ApiResult> FPSChecksAsync(CustInfoProfile custInfo, FPSChecksRequest fpsChecksRequest)
        {
            var apiResult = new ApiResult();
            var config = fpsServicesConfig.Value[custInfo.SiteName];
            string url = config.ResourceURL;

            Dictionary<string, string> sendIovationChecks = new Dictionary<string, string>
            {
                { "Account", custInfo.UserName },
                { "Action", fpsChecksRequest.action },
                { "ActionResult", fpsChecksRequest.actionResult },
                { "Subscriber",config.Subscriber},
                //{ "SubscriberDate",new DateTimeOffset(DateTime.Now).ToOffset(TimeSpan.FromHours(8)).ToString()},
                { "SubscriberDate",new DateTimeOffset(DateTime.Now).ToOffset(TimeSpan.FromHours(-4)).ToString()},
                { "Ip",fpsChecksRequest.ip},
                { "Url",fpsChecksRequest.url},
                { "AcceptTypes","text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*; q = 0.8,application / signed - exchange; v = b3; q = 0.9"},
                { "Platform",fpsChecksRequest.platform},
                { "UserAgent",fpsChecksRequest.userAgent},
                { "Screen",fpsChecksRequest.screen},
                { "__di",fpsChecksRequest.di}

            };

            var result = await httpHelper.HttpFromPostAsync(url, null, sendIovationChecks, "application/x-www-form-urlencoded");
            //var resultResp = JsonConvert.DeserializeObject<FPSResult>(result.Content.ReadAsStringAsync().Result);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
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
