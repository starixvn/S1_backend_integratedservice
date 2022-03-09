using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class TpUnsettleService
    {
        private readonly ILogger<TpUnsettleService> _logger;

        private readonly MySqlClient _mainDbClient;

        public TpUnsettleService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpUnsettleService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId, IDbTransaction transaction, DateTime? partitionTime = null)
        {
            return _mainDbClient.Execute("Service_TpUnsettle_Ins_V2",
                transaction: transaction,
                parameters: new { TpId = (int)tp, GameType = (int)gameType, TransId = transId, PartitionTime = partitionTime });
        }

    }
}