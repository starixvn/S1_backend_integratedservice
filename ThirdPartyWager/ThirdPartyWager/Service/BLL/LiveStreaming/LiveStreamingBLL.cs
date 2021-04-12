using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly LiveStreamingService liveStreamingService;

        public LiveStreamingBLL(LiveStreamingService liveStreamingService)
        {
            this.liveStreamingService = liveStreamingService;
        }
        public int SetLiveStreaming(string liveStreamingJson)
        {
            return liveStreamingService.SetLiveStreaming(liveStreamingJson);
        }
    }
}
