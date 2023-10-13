using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerCFService
    {
        private readonly IConfiguration configuration;

        public WagerCFService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetCF bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerCF_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public WagerCFModel GetWagerTotal(long beginTime, long endTime)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<WagerCFModel>("Service_TpWagerCFTotal_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        }
    }
}
