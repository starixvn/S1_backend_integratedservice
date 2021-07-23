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
    public class WagerSGService
    {
        private readonly IConfiguration configuration;

        public WagerSGService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetSG bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerSG_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public int InsertWagerRng(BetSG bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerRng_SG_Ins", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
