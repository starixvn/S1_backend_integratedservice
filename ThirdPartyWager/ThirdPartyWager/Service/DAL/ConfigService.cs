using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class ConfigService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _wagerDbClient;

        public ConfigService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);        
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null, DataBase dataBase = DataBase.Main)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<T>(sql, parameters),                
                DataBase.Wager => _wagerDbClient.Query<T>(sql, parameters),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
