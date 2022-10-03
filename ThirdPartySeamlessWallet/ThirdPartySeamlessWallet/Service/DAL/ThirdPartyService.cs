using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class ThirdPartyService
    {
        private readonly IConfiguration configuration;
        public ThirdPartyService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public TpCheck ThirdPartyCheck(int tpId, string tpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCheck>("Service_TpCheck_Get", new { TpId = tpId, TpUserName = tpUserName }, commandType: CommandType.StoredProcedure);
        }

        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ThirdPartySetting>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
        }
        public ThirdPartySetting GetThirdPartyBySite(int tpId, int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ThirdPartySetting>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BonusCategory> GetBonusCategoryList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BonusCategory>("Service_BonusCategory_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BonusMinBet> GetBonusMinBetList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BonusMinBet>("Service_BonusMinBet_Get", commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<TpGameCasino> GetGameCasino(int tpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<TpGameCasino>("Service_GameCasino_Get_V2", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetFailureRecord(TpBetFailureRecord tpBetFailureRecord)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_TpBetFailureRecord_Ins", tpBetFailureRecord, commandType: CommandType.StoredProcedure);
        }
    }
}
