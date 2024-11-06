using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerPPService
    {
        private readonly MySqlClient _mainDbClient;

        public WagerPPService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<BettransData_MaxGame> GetPendingList(TimeCast ticketTime, int gameType)   
        {
            var spName = gameType ==(int)GameType.PP_RNG ? "Service_BettransRNG_PP_Bet_Pending_Get" : "Service_BettransLC_PP_Bet_Pending_Get";
            return _mainDbClient.Query<BettransData_MaxGame>(spName, parameters: new { _BeginTime = ticketTime.startUnix, _EndTime = ticketTime.endUnix });
        }
    }
}
