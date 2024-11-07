using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WageSBPKenoService
    {
        private readonly MySqlClient _mainDbClient;

        public WageSBPKenoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<BettransData_MaxGame> GetPendingList(TimeCast ticketTime)
        {
            return _mainDbClient.Query<BettransData_MaxGame>("Service_BettransRng_Keno_Bet_Pending_Get", parameters: new { _BeginTime = ticketTime.startUnix, _EndTime = ticketTime.endUnix });
        }
    }
}
