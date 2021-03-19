using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class LiveStreamingController : ControllerBase
    {
        private readonly LiveStreamingBLL liveStreamingBLL;
        public LiveStreamingController(LiveStreamingBLL liveStreamingBLL)
        {
            this.liveStreamingBLL = liveStreamingBLL;
        }
        [HttpPost]
        public async Task<ApiResult> GetLiveStreamingLinkAsync(LiveStreamingLinkReq liveStreamingLinkReq )
        {
            ApiResult apiResult = new ApiResult();

            if (liveStreamingLinkReq.Ch != 0 && !string.IsNullOrEmpty(liveStreamingLinkReq.SiteName.Trim()) && !string.IsNullOrEmpty(liveStreamingLinkReq.SiteName.Trim()))
            {
                var resultResp = await liveStreamingBLL.GetLiveStreamingLink(liveStreamingLinkReq);
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
