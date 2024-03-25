using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class TpUnsettleService
    {
        private readonly MySqlClient _mainDbClient;

        public TpUnsettleService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId, DateTime? partitionTime = null)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Ins_V2",
                new { TpId = (int)tp, GameType = (int)gameType, TransId = transId, PartitionTime = partitionTime });
        }

        public int BatchInsertTpUnsettle(BatchTpUnsettleModel input)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Batch_Ins",
                new
                {
                    _TpId = (int)input.Tp,
                    _GameType = (int)input.GameType,
                    _Infos = JsonConvert.SerializeObject(input.Infos),
                    _PartitionTime = input.PartitionTime
                });
        }

        public IEnumerable<TpUnsettle> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount)
        {
            return _mainDbClient.Query<TpUnsettle>("Service_TpUnsettle_Get", parameters: new { TpId = (int)tp, GameType = (int)gameType, LimitCount });
        }
    }
}