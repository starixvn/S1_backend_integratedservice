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
    public class WagerAllBetService
    {
        private readonly IConfiguration configuration;

        public WagerAllBetService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWagerAllBet(BetAllBet bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerAllBet_Ins", bet, commandType: CommandType.StoredProcedure);
        }

    }
}
