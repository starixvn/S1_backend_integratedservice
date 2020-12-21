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
    public class WagerIconicService
    {
        private readonly IConfiguration configuration;

        public WagerIconicService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetIconic bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerFish_Iconic_Ins", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
