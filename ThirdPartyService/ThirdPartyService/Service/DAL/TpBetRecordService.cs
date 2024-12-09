using Dapper;

using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpBetRecordService : ITpBetRecordProvider
    {
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;
        private readonly IReadOnlyDictionary<int, List<GameCasinoOption>> _gameCasinoList;
        private readonly IReadOnlyDictionary<int, TpListOption> _tpList;
        private readonly IEnumerable<GameNameInfo> _gameNameList;

        public TpBetRecordService(IServiceProvider provider)
        {
            _gameNameList = provider.GetRequiredService<IEnumerable<GameNameInfo>>();
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _gameCasinoList = provider.GetRequiredService<IEnumerable<GameCasinoOption>>().GroupBy(i => i.TpId)
                .ToDictionary(d => d.Key, d => d.ToList());
            _tpList = provider.GetRequiredService<IEnumerable<TpListOption>>().ToDictionary(d => d.Id, d => d);
        }

        public List<GameCasinoOption> GetGameCasinoList(int tpId)
        {
            return _gameCasinoList.TryGetValue(tpId, out var result) ? result : new List<GameCasinoOption>();
        }

        public string GetTpName(int tpId)
        {
            return _tpList.TryGetValue(tpId, out var result) ? result.TpName : string.Empty;
        }

        public TpBetRecordDetailModel GetTpBetRecordDetail(int TpId, int GameType, string TransId, DateTime SettledTime)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    TpId,
                    GameType,
                    TransId,
                    SettledTime
                });

                return _reportDbClient.QueryFirst<TpBetRecordDetailModel>("Service_TpBetRecordDetail_Get_V2", p);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IReadOnlyDictionary<string, object> DbQueryFirst(string spName, object parameters, DataBase dataBase)
        {
            try
            {
                return dataBase switch
                {
                    DataBase.Wager => (IReadOnlyDictionary<string, object>)_wagerDbClient.QueryFirst(spName, parameters),
                    DataBase.Report => (IReadOnlyDictionary<string, object>)_reportDbClient.QueryFirst(spName, parameters),
                    DataBase.Main => (IReadOnlyDictionary<string, object>)_mainDbClient.QueryFirst(spName, parameters),
                };
            }
            catch (Exception)
            {
                return default;
            }
        }

        public IEnumerable<IReadOnlyDictionary<string, object>> DbQuery(string spName, object parameters, DataBase dataBase)
        {
            try
            {
                return dataBase switch
                {
                    DataBase.Wager => (IEnumerable<IReadOnlyDictionary<string, object>>)_wagerDbClient.Query(spName, parameters),
                    DataBase.Report => (IEnumerable<IReadOnlyDictionary<string, object>>)_reportDbClient.Query(spName, parameters),
                    DataBase.Main => (IEnumerable<IReadOnlyDictionary<string, object>>)_mainDbClient.Query(spName, parameters),
                };
            }
            catch (Exception)
            {
                return default;
            }
        }

        public GameNameInfo GetGameNameInfo(int tpId, string gameId, string bettype, string lang)
        {
            return _gameNameList.FirstOrDefault(g =>
                g.TpId == tpId && g.GameId == gameId && g.BetType == bettype && g.Lang == lang);
        }

        public GameNameInfo GetGameNameInfo(int tpId, string gameId, string lang)
        {
            return _gameNameList.FirstOrDefault(g =>
                g.TpId == tpId && g.GameId == gameId && g.Lang == lang);
        }
    }
}
