using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerDGService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerDGService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWagerDG(Bet bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerDG_Ins", new DynamicParameters(bet));
        }

        public WagerDGTotal GetWagerDGTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerDGTotal>("Service_TpWagerDGTotal_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime }));
        }
    }
}
