using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace ThirdPartyWager.Service.DAL
{
    public class SabaOddsService
    {
        private readonly IConfiguration _configuration;

        public SabaOddsService(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public int SetSabaOdds(string sabaOddsJson)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_SabaOdds_Set", new { SabaOddsJson = sabaOddsJson }, commandType: CommandType.StoredProcedure);
        }
    }
}
