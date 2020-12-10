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

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByCurrency(int tpId, int type, string currency)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<TpWagerTimerReferenceModel>("Service_TpWagerTimerReference_ByCurrency_Get", new { TpId = tpId, Type = type, Currency = currency }, commandType: CommandType.StoredProcedure);
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByGameType(int tpId, int type, int gameType)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<TpWagerTimerReferenceModel>("Service_TpWagerTimerReference_ByGameType_Get", new { TpId = tpId, Type = type, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }

        public int SetThirdPartyTimerByCurrency(int tpId, int type, long start, string currency)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerTimerReference_Currency_Set", new { TpId = tpId, Type = type, StartUnix = start, Currency = currency }, commandType: CommandType.StoredProcedure);
        }

        public int SetThirdPartyTimerByGameType(int tpId, int type, long start, int gameType)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerTimerReference_GameType_Set", new { TpId = tpId, Type = type, StartUnix = start, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }
    }
}
