using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSkywindService
    {
        private readonly IConfiguration configuration;

        public WagerSkywindService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetSkywind bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerSkywind_Ins", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
