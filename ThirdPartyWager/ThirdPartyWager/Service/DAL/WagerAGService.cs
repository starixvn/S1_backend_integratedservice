using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAGService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerAGService(
            IServiceProvider provider,
            IConfiguration configuration
        )
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            _wagerDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringWager"), log);
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), log);
        }

        public bool InsertWager(BetAG bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerAG_Ins", new DynamicParameters(bet));
        }

        public WagerAGTotal GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerAGTotal>("Service_TpWagerAGTotal_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }

        public IEnumerable<BettransDataAG> GetPendingList()
        {
            return _mainDbClient.Query<BettransDataAG>("Service_BettransLC_AGV2_Bet_Pending_Get");
        }
    }
}
