using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerDGService
    {
        private readonly IConfiguration configuration;

        public WagerDGService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWagerDG(Bet bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("TpWagerDG_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public WagerDGTotal GetWagerDGTotal(long beginTime, long endTime)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<WagerDGTotal>("Service_TpWagerDGTotal_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        }
    }
}
