using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ThirdPartyWager.Service.DAL
{
    public class LiveStreamingService
    {
        private readonly IConfiguration _configuration;

        public LiveStreamingService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public int SetLiveStreaming(string liveStreamingJson)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_LiveStreaming_Set", new { LiveStreamingJson = liveStreamingJson }, commandType: CommandType.StoredProcedure);
        }
    }
}
