using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMPService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerMPService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertWager(Bet_MP bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerPK_MP_Ins", new DynamicParameters(bet));
        }

        public WagerMPTotal GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerMPTotal>("Service_TpWagerPK_MP_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }
    }
}
