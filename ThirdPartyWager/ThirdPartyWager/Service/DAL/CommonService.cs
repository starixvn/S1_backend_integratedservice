using Dapper;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class CommonService
    {
        private readonly IEnumerable<ServerInfo> _serverInfos;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _wagerDbClient;

        public CommonService(IServiceProvider provider)
        {
            _serverInfos = provider.GetRequiredService<IEnumerable<ServerInfo>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<ServerInfo> GetSiteList()
        {
            return _serverInfos;
        }

        public IEnumerable<TpCurrency> GetTpCurrencyList(int tpId)
        {
            return _mainDbClient.Query<TpCurrency>("Service_TpCurrency_Get", new { TpId = tpId });
        }

        public IEnumerable<int> GetSiteByTpGameType(int gameType)
        {
            return _mainDbClient.Query<int>("Service_SiteByTpGameType_Get", new { TpGameType = gameType });
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummary(string spName, long beginTime, long endTime)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime }));
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummaryBySiteIds(string spName, long beginTime, long endTime, string siteIds)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime, _SiteIds = siteIds }));
        }

        public Task<IEnumerable<FincanceDailySummary>> GetWagerSummaryBySiteId(string spName, long beginTime, long endTime, string siteId)
        {
            return _wagerDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime, _SiteId = siteId }));
        }

        public Task<IEnumerable<FincanceDailySummary>> GetBettransSummary(string spName, long beginTime, long endTime)
        {
            return _mainDbClient.QueryAsync<FincanceDailySummary>(spName, new DynamicParameters(new { _BeginTime = beginTime, _EndTime = endTime }));
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

