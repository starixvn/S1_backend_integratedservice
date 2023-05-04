using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerTFGService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;
        public WagerTFGService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public Task<int> InsertWager(WagerTFGModel data)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerTFG_Ins", new DynamicParameters(data));
        }

        public Task<IEnumerable<TFGUnsettleCheck>> GetUnsettleCheckList()
        {
            return _mainDbClient.QueryAsync<TFGUnsettleCheck>("Service_TFG_Unsettle_Check", null);
        }

        public Task<TFGBettrans> GetBettrans(string id)
        {
            return _mainDbClient.QueryFirstAsync<TFGBettrans>("Service_BettransSports_TFG_Check", new DynamicParameters(new { id }));
        }
    }
}
