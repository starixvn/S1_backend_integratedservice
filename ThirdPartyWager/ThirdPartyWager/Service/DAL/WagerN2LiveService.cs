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
    public class WagerN2LiveService
    {
        private readonly IConfiguration configuration;

        public WagerN2LiveService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetN2Live bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            if (bet.schemeid != 0)
            {
                return conn.Execute("Service_TpSmartWagerLC_N2Live_Ins", bet, commandType: CommandType.StoredProcedure);
            }
            else
            {
                return conn.Execute("Service_TpWagerLC_N2Live_Ins", bet, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
