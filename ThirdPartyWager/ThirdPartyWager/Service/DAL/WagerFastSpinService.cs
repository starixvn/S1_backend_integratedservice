using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerFastSpinService
    {
        private readonly MySqlClient _mainDbClient;

        public WagerFastSpinService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<BettransData_MaxGame> GetPendingList()
        {
            return _mainDbClient.Query<BettransData_MaxGame>("Service_BettransRng_FastSpin_Bet_Pending_Get");
        }
    }
}
