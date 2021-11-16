using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class SabaOddsService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mySqlClient;

        public SabaOddsService(IConfiguration configuration,IServiceProvider provider)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionString");
            _mySqlClient = new MySqlClient(conn, log);
        }
        public async Task<int> SetSabaOdds(IEnumerable<UpdateOdd> sabaOdds)
        {
            return await _mySqlClient.ExecuteAsync("Service_SabaOdds_Set_V2", sabaOdds);
        }
        
        public IEnumerable<int> GetSabaOddsMatchId()
        {
            return _mySqlClient.Query<int>("Service_SabaOddsMatchId_Get");
        }
    }
}
