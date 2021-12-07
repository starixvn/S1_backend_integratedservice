using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class SabaOddsService
    {
        private readonly MySqlClient _mySqlClient;

        public SabaOddsService(IServiceProvider provider)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = provider.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString");
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
