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
        private static ConcurrentDictionary<int, ThirdPartySetting> ThirdPartySettingContainer = new ConcurrentDictionary<int, ThirdPartySetting>();
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
            if (!ThirdPartySettingContainer.ContainsKey(tpId))
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                var thirdPartyModel = conn.QueryFirstOrDefault<ThirdPartySetting>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
                if (thirdPartyModel != null)
                    ThirdPartySettingContainer.TryAdd(tpId, thirdPartyModel);
            }

            return ThirdPartySettingContainer.ContainsKey(tpId) ? ThirdPartySettingContainer[tpId] : null;
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
    }
}
