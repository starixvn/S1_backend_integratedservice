using System.Data;

using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpUnsettleService
    {
        private readonly MySqlClient _mainDbClient;

        private readonly MySqlClient _wagerDbClient;

        public TpUnsettleService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public IEnumerable<TResult> GetUnsettledListBySpName<TResult>(string spName, DataBase dataBase = DataBase.Main)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<TResult>(spName),
                DataBase.Wager => _wagerDbClient.Query<TResult>(spName),
                _ => throw new NotImplementedException(),
            };
        }

        public int SetSettleFlag(string spName, object parameters, DataBase dataBase = DataBase.Main)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Execute(spName, parameters: parameters),
                DataBase.Wager => _wagerDbClient.Execute(spName, parameters: parameters),
                _ => throw new NotImplementedException(),
            };
        }

        public IEnumerable<TpUnsettle> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount)
        {
            return _mainDbClient.Query<TpUnsettle>("Service_TpUnsettle_Get", parameters: new { TpId = (int)tp, GameType = (int)gameType, LimitCount });
        }

        public IEnumerable<TResult> GetTpUnsettleDataBySpName<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return dataBase switch
            {
                DataBase.Main => _mainDbClient.Query<TResult>(spName, parameters: parameters),
                DataBase.Wager => _wagerDbClient.Query<TResult>(spName, parameters: parameters),
                _ => throw new NotImplementedException(),
            };
        }

        public int SetTpUnsettle(Tp tp, GameType gameType, string transId, bool isSuccess)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Set", parameters: new { TpId = (int)tp, GameType = (int)gameType, TransId = transId, IsSuccess = isSuccess });
        }

        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId, IDbTransaction transaction, DateTime? partitionTime = null)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Ins_V2",
                transaction: transaction,
                parameters: new { TpId = (int)tp, GameType = (int)gameType, TransId = transId, PartitionTime = partitionTime });
        }

        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Ins", parameters: new { TpId = (int)tp, GameType = (int)gameType, TransId = transId });
        }

        public int InsertTpUnsettlePlaceBetAndSettle(Tp tp, GameType gameType, string transId, IDbTransaction transaction, DateTime? partitionTime = null)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Ins_PlaceBetAndSettle",
                transaction: transaction,
                parameters: new { TpId = (int)tp, GameType = (int)gameType, TransId = transId, PartitionTime = partitionTime });
        }
    }
}
