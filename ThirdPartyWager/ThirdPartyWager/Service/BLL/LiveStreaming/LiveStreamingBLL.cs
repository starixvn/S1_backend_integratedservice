using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly LiveStreamingService _liveStreamingService;

        public LiveStreamingBLL(IServiceProvider provider)
        {
            _liveStreamingService = provider.GetRequiredService<LiveStreamingService>();
        }
        public Task<int> SetLiveStreaming(IEnumerable<LiveStreamingJson> liveStreaming)
        {
            return _liveStreamingService.SetLiveStreaming(liveStreaming);
        }
        public int TruncateLiveStreaming()
        {
            return _liveStreamingService.TruncateLiveStreaming();
        }
    }
}
