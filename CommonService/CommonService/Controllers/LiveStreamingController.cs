using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class LiveStreamingController : ControllerBase
    {
        private readonly LiveStreamingBLL _liveStreamingBLL;

        public LiveStreamingController(IServiceProvider provider)
        {
            _liveStreamingBLL = provider.GetRequiredService<LiveStreamingBLL>();
        }

        [HttpPost]
        public async Task<ApiResult> GetLiveStreamingLinkAsync(LiveStreamingLinkReq liveStreamingLinkReq)
        {
            var apiResult = new ApiResult();

            if (liveStreamingLinkReq.Ch != 0 && !string.IsNullOrEmpty(liveStreamingLinkReq.SiteName.Trim()) && !string.IsNullOrEmpty(liveStreamingLinkReq.Uid.Trim()) && !string.IsNullOrEmpty(liveStreamingLinkReq.Ip.Trim()))
            {
                var resultResp = await _liveStreamingBLL.GetLiveStreamingLink(liveStreamingLinkReq);
                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
            }

            return apiResult;
        }
    }
}
