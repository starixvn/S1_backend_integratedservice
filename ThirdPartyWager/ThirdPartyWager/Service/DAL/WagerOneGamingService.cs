using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerOneGamingService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerOneGamingService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }
        public int InsertWager(BetOneGaming bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_OneGaming_Ins", bet);
        }
        
    }
}
