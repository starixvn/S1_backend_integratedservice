using System.Collections.Generic;
using System.Threading.Tasks;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly IOptions<Dictionary<string, LiveStreamingConfig>> liveStreamingConfig;
        private readonly HttpHelper httpHelper;

        public LiveStreamingBLL(IOptions<Dictionary<string, LiveStreamingConfig>> liveStreamingConfig, HttpHelper httpHelper)
        {
            this.liveStreamingConfig = liveStreamingConfig;
            this.httpHelper = httpHelper;
        }
        public async Task<ApiResult> GetLiveStreamingLink(LiveStreamingLinkReq liveStreamingLinkReq)
        {
            var apiResult = new ApiResult();
            var config = liveStreamingConfig.Value[liveStreamingLinkReq.SiteName.Trim()];
            string url = config.ApiURL;
            Dictionary<string, string> liveStreaming = new Dictionary<string, string>
            {
                { "action", config.Action },
                { "apiuser", config.Apiuser},
                { "key", config.Key},
                { "ch", liveStreamingLinkReq.Ch.ToString()},
                { "uid", liveStreamingLinkReq.Uid.Trim()},
                { "ip", liveStreamingLinkReq.Ip.Trim()}

            };

            var result = await httpHelper.HttpGetAsync(url, null, liveStreaming, null);
            var resultResp = JsonConvert.DeserializeObject<LiveStreamingLinkResult>(result.Content.ReadAsStringAsync().Result); ;

            if (result.StatusCode == System.Net.HttpStatusCode.OK && resultResp.Status == "OK")
            {
                apiResult.ErrorCode = 0;
                apiResult.Message = resultResp.H5LINKROW;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = "Error";
            }

            return apiResult;

        }
    }
}
