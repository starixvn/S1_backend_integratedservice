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

        public ThirdPartyHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public ThirdPartyModel GetThirdPartySetting(int tpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<ThirdPartyModel>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
        }

        public ThirdPartyTimerModel GetThirdPartyTimer(int tpId, int type)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<ThirdPartyTimerModel>("Service_TpWagerTimer_Get", new { TpId = tpId, Type = type }, commandType: CommandType.StoredProcedure);
        }

        public int SetThirdPartyTimer(int tpId, int type, long start)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerTimer_Ins", new { TpId = tpId, Type = type, StartUnix = start }, commandType: CommandType.StoredProcedure);
        }
    }
}
