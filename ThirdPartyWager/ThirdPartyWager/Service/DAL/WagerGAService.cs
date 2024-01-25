using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerGAService
    {
        private readonly MySqlClient _wagerDbClient;
        public WagerGAService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(GATickets data)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWager_GA_Ins", new DynamicParameters(data));
        }
    }
}
