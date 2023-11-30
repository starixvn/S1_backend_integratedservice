using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerNLCService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerNLCService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public Task<int> InsertWager(Wager_EVO bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerRng_NLC_Ins", new DynamicParameters(bet));
        }

        public Task<int> InsertWagerRoundInfo(WagerRoundInfo_EVO round)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerRoundInfoRng_NLC_Ins", new DynamicParameters(round));
        }

        public WagerRoundInfoTotal_EVO GetWagerRoundInfoTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerRoundInfoTotal_EVO>("Service_TpWagerRoundInfoRng_NLC_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }

        public Task<IEnumerable<SettleData>> GetTransIdList(string transId)
        {
            return _wagerDbClient.QueryAsync<SettleData>("Service_TpWagerRng_NLC_TransId_Get", new DynamicParameters(new { _TransIds = transId }));
        }
    }
}
