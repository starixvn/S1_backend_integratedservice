using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerJiLiService
    {
        private readonly MySqlClient _mainDbClient;

        public WagerJiLiService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<BettransData_JiLi> GetPendingList(TimeCast ticketTime)
        {
            return _mainDbClient.Query<BettransData_JiLi>("Service_BettransRNG_JiLi_Bet_Pending_Get", parameters: new { _BeginTime = ticketTime.startUnix, _EndTime = ticketTime.endUnix });
        }
    }
}
