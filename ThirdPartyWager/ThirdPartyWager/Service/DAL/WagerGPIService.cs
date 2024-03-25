using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerGPIService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerGPIService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(IEnumerable<Bet_GPI> bets, int gameType)
        {
            if (!Enum.IsDefined(typeof(GameType), gameType))
            {
                throw new ArgumentException("GAMETYPE_ERROR");
            }
            var spName = (gameType == (int)GameType.GPI_Keno) ? "Service_TpWagerLT_GPI_Ins" : "Service_TpWagerRng_GPI_Ins";
            return _wagerDbClient.Execute(spName, bets);
        }
    }
}
