using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly IConfiguration configuration;

        public TpCustInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName }, commandType: CommandType.StoredProcedure);
        }

        public int SetCustGotoGame(int custId, int tpId, int gameType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_CustGotoGame_Set", new { CustId = custId, TpId = tpId, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }
        public int GetCustGotoGameType(int custId, int tpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, TpId = tpId });
            p.Add("GameType", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = conn.Execute("Service_CustGotoGameType_Get", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("GameType");
        }
    }
}
