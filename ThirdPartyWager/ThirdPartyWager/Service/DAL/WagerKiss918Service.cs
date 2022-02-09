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

        public int InsertWager(BetKiss918 bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerKiss918_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<WagerKiss918Total> GetWagerTotal(long beginTime, long endTime, string currency)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<WagerKiss918Total>("Service_TpWagerKiss918_Total_Get", new { BeginTime = beginTime, EndTime = endTime, Currency = currency }, commandType: CommandType.StoredProcedure);
        }
    }
}
