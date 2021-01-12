using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerV8Service
    {
        private readonly IConfiguration configuration;

        public WagerV8Service(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetV8 bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_V8_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public WagerV8Total GetWagerTotal(long beginTime, long endTime)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<WagerV8Total>("Service_TpWagerPK_V8_Total_Get", new { BeginTime = beginTime, EndTime = endTime}, commandType: CommandType.StoredProcedure);
        }
    }
}
