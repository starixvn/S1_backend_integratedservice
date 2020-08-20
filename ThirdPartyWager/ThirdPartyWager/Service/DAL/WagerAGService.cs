using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAGService
    {
        private readonly IConfiguration configuration;

        public WagerAGService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetAG bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerAG_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        //public WagerSATotal GetWagerTotal(long beginTime, long endTime)
        //{
        //    using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
        //    return conn.QueryFirstOrDefault<WagerAGTotal>("Service_TpWagerAGTotal_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        //}
    }
}
