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

        /// <summary>
        /// 找到用戶還在Running的CustBonus
        /// </summary>
        public IEnumerable<CustBonus> GetCustBonusList(int CustId)
        {
            return _mainDbClient.Query<CustBonus>("Service_Bonus_Get_V2", parameters: new { CustId });
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
