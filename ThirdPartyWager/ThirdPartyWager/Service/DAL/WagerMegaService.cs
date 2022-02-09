using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMegaService
    {
        private readonly IConfiguration configuration;

        public WagerMegaService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(ReportMega bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerMega_Ins", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
