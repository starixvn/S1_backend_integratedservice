using System.Net;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly Dictionary<string, LiveStreamingConfig> _liveStreamingConfig;
        private readonly HttpHelper _httpHelper;

        public LiveStreamingBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _liveStreamingConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<LiveStreamingConfig>("LiveStreaming");
        }

        public async Task<ApiResult> GetLiveStreamingLink(LiveStreamingLinkReq liveStreamingLinkReq)
        {
            var apiResult = new ApiResult();
            var config = _liveStreamingConfig[liveStreamingLinkReq.SiteName.Trim()];
            var url = config.ApiURL;
            var liveStreaming = new Dictionary<string, string>
            {
                { "action", config.Action },
                { "apiuser", config.Apiuser},
                { "key", config.Key},
                { "ch", liveStreamingLinkReq.Ch.ToString()},
                { "uid", liveStreamingLinkReq.Uid.Trim()},
                { "ip", liveStreamingLinkReq.Ip.Trim()}

            };

            using var result = await _httpHelper.HttpGetAsync(url, null, liveStreaming, null);
            var resultResp = JsonConvert.DeserializeObject<LiveStreamingLinkResult>(result.Content.ReadAsStringAsync().Result); ;

            if (result.StatusCode == HttpStatusCode.OK && resultResp.Status == "OK")
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
