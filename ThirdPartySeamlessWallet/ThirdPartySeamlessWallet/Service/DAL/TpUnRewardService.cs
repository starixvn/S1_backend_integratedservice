using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpUnRewardService
    {
        private readonly ILogger<TpUnRewardService> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly MySqlClient _wagerDbClient;

        public TpUnRewardService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpUnRewardService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<TResult> GetTpUnRewardDataBySpName<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<TResult>(spName, parameters: parameters),
                DataBase.Wager => _wagerDbClient.Query<TResult>(spName, parameters: parameters),
                _ => throw new NotImplementedException(),
            };
        }

        public IEnumerable<TpUnReward> GetTpUnRewardList(Tp tp, GameType gameType, int LimitCount)
        {
            return _mainDbClient.Query<TpUnReward>("Service_TpUnReward_Get", parameters: new { TpId = (int)tp, GameType = (int)gameType, LimitCount });
        }

        public int SetTpUnReward(Tp tp, GameType gameType, string transId, bool isSuccess)
        {
            return _mainDbClient.Execute("Service_TpUnReward_Set", parameters: new
            {
                TpId = (int)tp,
                GameType = (int)gameType,
                TransId = transId,
                IsSuccess = isSuccess
            });
        }

        public int InsertTpUnReward(Tp tp, GameType gameType, string transId, IDbTransaction transaction, DateTime? partitionTime = null)
        {
            return _mainDbClient.Execute("Service_TpUnReward_Ins", transaction: transaction, parameters: new
            {
                TpId = (int)tp,
                GameType = (int)gameType,
                TransId = transId,
                PartitionTime = partitionTime
            });
        }
    }
}
