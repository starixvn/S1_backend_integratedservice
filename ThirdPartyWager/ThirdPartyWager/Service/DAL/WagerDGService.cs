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
    public class WagerDGService
    {
        private readonly IConfiguration configuration;

        public WagerDGService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWagerDG(IEnumerable<Bet> list)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("TpWagerDG_Ins", list, commandType: CommandType.StoredProcedure);
        }
    }
}
