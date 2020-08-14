using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class SettledService
    {
        private readonly IConfiguration configuration;

        public SettledService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public bool GetBonusIdByTpId(int tpId, int custId, out int bonusId)
        {
            bonusId = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { TpId = tpId, CustId = custId });
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute($"Service_BonusIdByTpId_Get", p, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                bonusId = p.Get<int>("BonusId");
            }

            return result > 0;
        }

        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BonusFulfill>("Service_BonusFulfill_Get", commandType: CommandType.StoredProcedure);
        }
        public int BonusSettle(BonusFulfill bonusFulfill)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            int result = conn.Execute("Service_BonusSettle_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
    }
}
