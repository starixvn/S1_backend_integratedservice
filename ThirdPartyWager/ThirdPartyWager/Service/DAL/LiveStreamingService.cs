using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mySqlClient;

        public LiveStreamingService(IConfiguration configuration,IServiceProvider provider)
        {
            _configuration = configuration;
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionString");
            _mySqlClient = new MySqlClient(conn, log);
        }
        public async Task<int> SetLiveStreaming(IEnumerable<LiveStreamingJson> liveStreaming)
        {
            await using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return await _mySqlClient.ExecuteAsync("Service_LiveStreaming_Set_V2", liveStreaming );
        }
    }
}
