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
    public class ThirdPartyHelper
    {

        private readonly IConfiguration configuration;

        public ThirdPartyHelper( IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public ThirdPartyModel GetThirdPartySetting(int tpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ThirdPartyModel>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
        }
    }
}
