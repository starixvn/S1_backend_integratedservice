using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class SettledService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public SettledService(IConfiguration configuration, ILogger<MySqlClient> logger)
        {
            this.configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }
        public bool GetBonusIdByTpId(int tpId, int custId, out int bonusId)
        {
            bonusId = 0;
            var p = new DynamicParameters(new { TpId = tpId, CustId = custId });
            p.Add("BonusId", direction: ParameterDirection.Output);
            var result = _reportDbClient.Execute($"Service_BonusIdByTpId_Get", p);
            if (result)
            {
                bonusId = p.Get<dynamic>("BonusId") ?? 0;
            }

            return bonusId != 0;
        }

        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return _reportDbClient.Query<BonusFulfill>("Service_BonusFulfill_Get");
        }
        public bool BonusSettle(BonusFulfill bonusFulfill)
        {
            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            return _mainDbClient.Execute("Service_BonusSettle_Set", p);
        }
    }
}
