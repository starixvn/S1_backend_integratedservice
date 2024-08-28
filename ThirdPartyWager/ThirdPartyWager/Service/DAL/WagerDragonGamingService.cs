using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerDragonGamingService
    {
        private readonly MySqlClient _wagerDbClient;
        public WagerDragonGamingService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(WonCasinoDetailData data)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWager_WonCasino_Ins", new DynamicParameters(data));
        }
    }
}
