using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly MySqlClient _mySqlClient;

        public LiveStreamingService(IServiceProvider provider)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = provider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
            _mySqlClient = new MySqlClient(conn, log);
        }
        public async Task<int> SetLiveStreaming(IEnumerable<LiveStreamingJson> liveStreaming)
        {
            return await _mySqlClient.ExecuteAsync("Service_LiveStreaming_Set_V2", liveStreaming );
        }
        public int TruncateLiveStreaming()
        {
            return _mySqlClient.Execute("Service_TruncateLiveStreaming_Set");
        }
    }
}
