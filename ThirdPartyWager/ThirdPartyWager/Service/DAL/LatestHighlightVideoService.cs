using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class LatestHighlightVideoService
    {
        private readonly IConfiguration _configuration;

        public LatestHighlightVideoService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_LatestHighlightVideoV2_Set", data, commandType: CommandType.StoredProcedure);
        }
        public int GetLatestHighlightVideo(int siteId, string date, string title)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<int>("Service_LatestHighlightVideo_Get", new { SiteId = siteId, Date = date, Title = title }, commandType: CommandType.StoredProcedure);
        }
    }
}
