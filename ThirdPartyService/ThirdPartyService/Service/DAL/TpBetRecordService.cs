using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpBetRecordService : ITpBetRecordProvider
    {
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly IReadOnlyDictionary<int, List<GameCasinoOption>> _GameCasinoList;

        public TpBetRecordService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _GameCasinoList = provider.GetRequiredService<IEnumerable<GameCasinoOption>>().GroupBy(i => i.TpId)
                .ToDictionary(d => d.Key, d => d.ToList());
        }

        public List<GameCasinoOption> GetGameCasinoList(int tpId)
        {
            return _GameCasinoList.TryGetValue(tpId, out var result) ? result : null;
        }


        public TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetail getBetDetail)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    TpId = getBetDetail.TpGameTypeOption.TpId,
                    GameType = getBetDetail.TpGameTypeOption.Type,
                    TransId = getBetDetail.TransId,
                    SettledTime = getBetDetail.SettledTime
                });

                return _reportDbClient.QueryFirst<TpBetRecordDetailModel>("Service_TpBetRecordDetail_Get_V2", p);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IReadOnlyDictionary<string, object> WagerDbQueryFirst(string spName, object parameters = null)
        {
            try
            {
                return (IReadOnlyDictionary<string, object>)_wagerDbClient.QueryFirst(spName, parameters);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public IReadOnlyDictionary<string, object> ReportDbQueryFirst(string spName, object parameters = null)
        {
            try
            {
                return (IReadOnlyDictionary<string, object>)_reportDbClient.QueryFirst(spName, parameters);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public IEnumerable<IReadOnlyDictionary<string, object>> WagerDbQuery(string spName, object parameters = null)
        {
            try
            {
                return (IEnumerable<IReadOnlyDictionary<string, object>>)_wagerDbClient.Query(spName, parameters);
            }
            catch (Exception)
            {
                return default;
            }
        }

        public IEnumerable<IReadOnlyDictionary<string, object>> ReportDbQuery(string spName, object parameters = null)
        {
            try
            {
                return (IEnumerable<IReadOnlyDictionary<string, object>>)_reportDbClient.Query(spName, parameters);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
