using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAllBetService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerAllBetService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool InsertWager(BetAllBet bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerAllBet_Ins", new DynamicParameters(bet));
        }

        public WagerAllBetTotal GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerAllBetTotal>("Service_TpWagerAllBetTotal_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }

        public Task<RewardAllBetData> GetRewardRecord(long eventRecordNum)
        {
            return _mainDbClient.QueryFirstAsync<RewardAllBetData>("Service_RewardLC_Allbet_Get", new DynamicParameters(new { eventRecordNum }));
        }
    }
}
