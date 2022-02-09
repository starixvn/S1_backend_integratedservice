using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerWMService
    {
        private readonly IConfiguration configuration;

        public WagerWMService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetWM bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerWM_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public WagerWMTotal GetWagerTotal(long beginTime, long endTime, string currency)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<WagerWMTotal>("Service_TpWagerWM_Total_Get", new { BeginTime = beginTime, EndTime = endTime, Currency = currency }, commandType: CommandType.StoredProcedure);
        }
    }
}
