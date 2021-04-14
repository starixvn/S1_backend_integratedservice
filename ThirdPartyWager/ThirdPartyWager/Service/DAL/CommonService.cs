using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class CommonService
    {
        private readonly IConfiguration _configuration;

        public CommonService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public IEnumerable<ServerInfo> GetSiteList()
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<ServerInfo>("Admin_ServerInfoList_Get", commandType: CommandType.StoredProcedure);
        }
    }
}

