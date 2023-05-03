using Dapper;
using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using System.Data;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class SettledService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<SettledService> _logger;

        public SettledService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _logger = provider.GetRequiredService<ILogger<SettledService>>();
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
            p.Add("TpId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BonusSettle_Set_V3", p, trans);
            if (result >= 0)
            {
                var bonus = p.Get<decimal>("Bonus");
                var tpId = p.Get<int>("TpId");
                
                if (bonus > 0 && tpId == (int)Tp.SBSW)
                {
                    var parameters = new DynamicParameters(new
                    {
                        CustId = bonusFulfill.CustId,
                        UserName = bonusFulfill.UserName,
                        SiteId = bonusFulfill.SiteId,
                        Amount = bonus,
                        Type = 4,
                        Memo = $"Bonus Transfer to Cash: {Math.Abs(bonus):#,##0.##}",
                        RefTransId = bonusFulfill.BonusId
                    });

                    var allowNegative = 1;
                    parameters.Add("AllowNegative", allowNegative, dbType: DbType.Decimal, direction: ParameterDirection.Input);
                    parameters.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                    result = _mainDbClient.Execute("Service_SeamlessWallet_Set_V2", parameters, trans);   
                    _logger.LogInformation($"SBSW Bonus Bonus Fulfill Transfer to Cash:{JsonConvert.SerializeObject(parameters)} | result:{result} | bonus:{bonus}");                    
                }

                if (result >= 0)
                {
                    trans.Commit();
                }
            }

            conn.Close();
            conn.Dispose();
            return result > 0;
        }
    }
}
