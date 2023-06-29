using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerPlayTechService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerPlayTechService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(Record_PlayTech bets)
        {
            return _wagerDbClient.Execute("Service_TpWager_PlayTech_Ins", bets);
        }
    }
}
