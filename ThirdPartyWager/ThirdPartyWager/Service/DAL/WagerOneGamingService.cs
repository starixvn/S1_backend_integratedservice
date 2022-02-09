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

        //public WagerOneGamingTotal GetWagerTotal(long beginTime, long endTime)
        //{
        //    using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
        //    return conn.QueryFirstOrDefault<WagerOneGamingTotal>("Service_TpWagerPK_OneGaming_Total_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        //}
    }
}
