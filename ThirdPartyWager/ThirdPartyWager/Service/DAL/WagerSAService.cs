using System.Data;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSAService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerSAService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }
        public int InsertWager(IEnumerable<BetSA> bets, IDbTransaction transaction)
        {
            return _wagerDbClient.Execute("Service_TpWagerSA_Ins", transaction, bets);
        }

        public IEnumerable<WagerSATotal> GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.Query<WagerSATotal>("Service_TpWagerSA_Total_Get_V2", new { BeginTime = beginTime, EndTime = endTime });
        }
    }
}
