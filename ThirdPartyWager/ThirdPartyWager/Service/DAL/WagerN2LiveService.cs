using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerN2LiveService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerN2LiveService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(BetN2Live bet)
        {
            if (bet.schemeid != 0)
            {
                return _wagerDbClient.ExecuteAsync("Service_TpSmartWagerLC_N2Live_Ins_V2", new DynamicParameters(bet));
            }
            else
            {
                return _wagerDbClient.ExecuteAsync("Service_TpWagerLC_N2Live_Ins_V2", new DynamicParameters(bet));
            }
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime }));
        }
    }
}
