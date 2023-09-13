using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
   public class TpCustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;

        public TpCustInfoService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            var mySqlLogger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), mySqlLogger);
        }
        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName });
            
        }
    }
}