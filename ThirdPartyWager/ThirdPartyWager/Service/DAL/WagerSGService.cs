using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSGService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerSGService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(BetSG bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerSG_Ins", bet);
        }

        public int InsertWagerRng(BetSG bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerRng_SG_Ins", bet);
        }
    }
}
