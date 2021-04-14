using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ThirdPartyWager.Service.DAL
{
    public class LatestHighlightVideoService
    {
        private readonly IConfiguration _configuration;

        public LatestHighlightVideoService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public int SetLatestHighlightVideo(string latestHighlightVideoJson)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_LatestHighlightVideo_Set", new { LatestHighlightVideoJson = latestHighlightVideoJson }, commandType: CommandType.StoredProcedure);
        }
        public int GetLatestHighlightVideo(int siteId, string date, string title)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            var a = conn.QueryFirstOrDefault<int>("Service_LatestHighlightVideo_Get", new { SiteId = siteId, Date = date, Title = title }, commandType: CommandType.StoredProcedure);
            return a;
        }

    }
}
