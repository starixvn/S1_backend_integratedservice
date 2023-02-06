using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class SettledService
    {
        private readonly MySqlClient _mainDbClient;

        public SettledService(IServiceProvider provider)
        {
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BonusSettle_Set", p, trans);
            if (result >= 0)
            {
                trans.Commit();
            }

            conn.Close();
            conn.Dispose();
            return result > 0;
        }
    }
}
