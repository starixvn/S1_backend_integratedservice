using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerEVOService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerEVOService(IServiceProvider provider, IConfiguration configuration)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionStringWager");
            _wagerDbClient = new MySqlClient(conn, log);
        }

        public Task<int> InsertWager(Wager_EVO bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerLC_EVO_Ins", new DynamicParameters(bet));
        }

        public Task<int> InsertWagerRoundInfo(WagerRoundInfo_EVO round)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerRoundInfoLC_EVO_Ins", new DynamicParameters(round));
        }

        public WagerRoundInfoTotal_EVO GetWagerRoundInfoTotal(long beginTime, long endTime)
        {
            
            return _wagerDbClient.QueryFirst<WagerRoundInfoTotal_EVO>("Service_TpWagerRoundInfoLC_EVO_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }
    }
}
