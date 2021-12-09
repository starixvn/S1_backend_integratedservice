using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpUnsettleService
    {
        private readonly ILogger<TpUnsettleService> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly MySqlClient _reportDbClient;

        private readonly MySqlClient _wagerDbClient;

        public TpUnsettleService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpUnsettleService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<TResult> GetUnsettledListBySpName<TResult>(string spName, DataBase dataBase = DataBase.Main)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<TResult>(spName),
                DataBase.Report => _reportDbClient.Query<TResult>(spName),
                DataBase.Wager => _wagerDbClient.Query<TResult>(spName),
                _ => throw new NotImplementedException(),
            };
        }

        public int SetSettleFlagBySpName(string spName, object parameters, DataBase dataBase = DataBase.Main)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Execute(spName, parameters: parameters),
                DataBase.Report => _reportDbClient.Execute(spName, parameters: parameters),
                DataBase.Wager => _wagerDbClient.Execute(spName, parameters: parameters),
                _ => throw new System.NotImplementedException(),
            };
        }

        public IEnumerable<string> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount)
        {
            return _mainDbClient.Query<string>("Service_TpUnsettle_Get", parameters: new { TpId = (int)tp, GameType = (int)gameType, LimitCount });
        }

        public IEnumerable<TResult> GetTpUnsettleDataBySpName<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<TResult>(spName, parameters: parameters),
                DataBase.Report => _reportDbClient.Query<TResult>(spName, parameters: parameters),
                DataBase.Wager => _wagerDbClient.Query<TResult>(spName, parameters: parameters),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
