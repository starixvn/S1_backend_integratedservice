using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class LiveStreamingBLL
    {
        private readonly LiveStreamingService liveStreamingService;

        public LiveStreamingBLL(LiveStreamingService liveStreamingService)
        {
            this.liveStreamingService = liveStreamingService;
        }
        public Task<int> SetLiveStreaming(IEnumerable<LiveStreamingJson> liveStreaming)
        {
            return liveStreamingService.SetLiveStreaming(liveStreaming);
        }
    }
}
