using Dapper;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerAPService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerAPService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public bool InsertWager(Bet_AdvantPlayMini bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerRng_AP_Mini_Ins", new DynamicParameters(bet));
        }
    }
}
