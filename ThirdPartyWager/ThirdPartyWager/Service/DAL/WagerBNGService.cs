using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerBNGService
    {
        private readonly MySqlClient _wagerDbClient;
        public WagerBNGService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(item data)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWager_FaChai_Ins", new DynamicParameters(data));
        }
    }
}
