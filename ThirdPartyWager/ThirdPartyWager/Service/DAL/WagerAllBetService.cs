using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAllBetService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerAllBetService(IServiceProvider provider, IConfiguration configuration)
        {
            this.configuration = configuration;
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var wager = configuration.GetConnectionString("ConnectionStringWager");
            var main = configuration.GetConnectionString("ConnectionString");
            _wagerDbClient = new MySqlClient(wager, log);
            _mainDbClient = new MySqlClient(main, log);
        }

        public int InsertWager(BetAllBet bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerAllBet_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public WagerAllBetTotal GetWagerTotal(long beginTime, long endTime)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<WagerAllBetTotal>("Service_TpWagerAllBetTotal_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        }

        public Task<RewardAllBetData> GetRewardRecord(long eventRecordNum)
        {
            return _mainDbClient.QueryFirstAsync<RewardAllBetData>("Service_RewardLC_Allbet_Get", new DynamicParameters(new { eventRecordNum }));
        }
    }
}
