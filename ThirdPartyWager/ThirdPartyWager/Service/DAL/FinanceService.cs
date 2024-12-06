using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class FinanceService
    {
        private readonly MySqlClient _mainDbClient;


        public FinanceService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

        }

        public int SetFinanceTpDailySummary(IEnumerable<FincanceDailySummary> dailySummary)
        {
            return _mainDbClient.Execute("Finance_Tp_DailySummary_Set", dailySummary);
        }

        public int SetFinanceTpDailySummary(FincanceDailySummary dailySummary)
        {
            // TODO
            return _mainDbClient.Execute("Finance_Tp_DailySummary_Set", dailySummary);
        }
    }
}
