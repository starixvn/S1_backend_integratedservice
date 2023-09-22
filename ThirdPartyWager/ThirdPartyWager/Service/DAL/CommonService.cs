using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class CommonService
    {
        private readonly IEnumerable<ServerInfo> _serverInfos;
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _wagerDbClient;

        public CommonService(IServiceProvider provider)
        {
            _serverInfos = provider.GetRequiredService<IEnumerable<ServerInfo>>();
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<ServerInfo> GetSiteList()
        {
            return _serverInfos;
        }

        public IEnumerable<TpCurrency> GetTpCurrencyList(int tpId)
        {
            return _reportDbClient.Query<TpCurrency>("Service_TpCurrency_Get", new { TpId = tpId });
        }
        public IEnumerable<int> GetSiteByTpGameType(int gameType)
        {
            return _reportDbClient.Query<int>("Service_SiteByTpGameType_Get", new { TpGameType = gameType });
        }
        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime }));
        }

        public int InsertTpWagerRedo(TpWagerRedo redo)
        {
            return _wagerDbClient.Execute("Service_TpWagerRedo_Ins", redo);
        }

        public IEnumerable<TpWagerRedo> GetTpWagerRedo(int tpId)
        {
            return _wagerDbClient.Query<TpWagerRedo>("Service_TpWagerRedo_Get", new { TpId = tpId });
        }

        public int DeleteTpWagerRedo(TpWagerRedo redo)
        {
            return _wagerDbClient.Execute("Service_TpWagerRedo_Del", redo);
        }
    }
}

