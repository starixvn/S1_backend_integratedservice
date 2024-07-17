using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.DAL
{
    public class CurrencyService
    {
        private readonly MySqlClient _mainDbClient;

        public CurrencyService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public IEnumerable<Currency> GetCurrency()
        {
            return _mainDbClient.Query<Currency>("Common_Currency_Get" ,null);
        }
    }
}
