using System.Data;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSimplePlayService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerSimplePlayService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(IEnumerable<Bet_SimplePlay> bets)
        {
            return _wagerDbClient.Execute("Service_TpWagerRng_SimplePlay_Ins", bets);
        }
    }
}
