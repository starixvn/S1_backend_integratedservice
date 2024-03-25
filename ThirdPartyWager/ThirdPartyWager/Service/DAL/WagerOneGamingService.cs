using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerOneGamingService
    {
        private readonly IConfiguration configuration;

        public WagerOneGamingService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int InsertWager(BetOneGaming bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_OneGaming_Ins", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
