using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKiss918Service
    {
        private readonly IConfiguration configuration;

        public WagerKiss918Service(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(WagerKiss918Model data)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerKiss918_Ins", data, commandType: CommandType.StoredProcedure);
        }
    }
}
