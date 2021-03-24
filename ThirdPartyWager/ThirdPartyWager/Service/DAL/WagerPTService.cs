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
    public class WagerPTService
    {
        private readonly IConfiguration configuration;

        public WagerPTService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetPT bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            if (string.Equals(bet.GAMETYPE, "Live Games", StringComparison.OrdinalIgnoreCase))
            {
                // live casino
                return conn.Execute("Service_TpWagerLC_PT_Ins", bet, commandType: CommandType.StoredProcedure);
            }
            else
            {
                // slot
                return conn.Execute("Service_TpWagerRng_PT_Ins", bet, commandType: CommandType.StoredProcedure);
            }

        }
    }
}
