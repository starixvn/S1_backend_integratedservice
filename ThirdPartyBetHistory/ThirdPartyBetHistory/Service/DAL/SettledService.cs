using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class SettledService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly string _connectionString;

        public SettledService(IServiceProvider provider)
        {
            _connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public bool GetBonusIdByTpId(int tpId, int custId, out int bonusId)
        {
            bonusId = 0;
            var p = new DynamicParameters(new { TpId = tpId, CustId = custId });
            p.Add("BonusId", direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute($"Service_BonusIdByTpId_Get", p);
            if (result > 0)
            {
                bonusId = p.Get<int?>("BonusId") ?? 0;
            }

            return result > 0 && bonusId != 0;
        }

        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return _mainDbClient.Query<BonusFulfill>("Service_BonusFulfill_Get");
        }
        public bool BonusSettle(BonusFulfill bonusFulfill)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BonusSettle_Set", p, conn, trans);
            if (result >= 0)
            {
                trans.Commit();
            }

            return result > 0;
        }
    }
}
