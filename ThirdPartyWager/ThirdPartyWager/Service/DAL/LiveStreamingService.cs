using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly MySqlClient _mySqlClient;

        public LiveStreamingService(IServiceProvider provider)
        {
            _mySqlClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public async Task<int> SetLiveStreaming(IEnumerable<LiveStreamingJson> liveStreaming)
        {
            return await _mySqlClient.ExecuteAsync("Service_LiveStreaming_Set_V2", liveStreaming );
        }
        public int TruncateLiveStreaming()
        {
            return _mySqlClient.Execute("Service_TruncateLiveStreaming_Del");
        }
    }
}
