using CommonService.Service.Library;
using CommonService.Service.Model.enums;

namespace CommonService.Service.DAL
{
    public class ConfigService
    {
        private readonly MySqlClient _mainDbClient;

        public ConfigService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            return _mainDbClient.Query<T>(sql, parameters);
        }
    }
}
