using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class LatestHighlightVideoService
    {
        private readonly IConfiguration _configuration;

        public LatestHighlightVideoService(IServiceProvider provider)
        {
            _configuration = provider.GetRequiredService<IConfiguration>();
        }
        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_LatestHighlightVideo_Set_V2", data, commandType: CommandType.StoredProcedure);
        }
        public int GetLatestHighlightVideo(int siteId, string date, string title)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<int>("Service_LatestHighlightVideo_Get", new { SiteId = siteId, Date = date, Title = title }, commandType: CommandType.StoredProcedure);
        }
    }
}
