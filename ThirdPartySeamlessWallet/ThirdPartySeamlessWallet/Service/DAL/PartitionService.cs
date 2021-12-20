using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class PartitionService
    {
        private readonly ILogger<PartitionService> _logger;
        private readonly MySqlClient _mainDbClient;

        public PartitionService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PartitionService>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public IEnumerable<T> Query<T>(DateTime partitionTime, string spName, object parameters, int partitionCheckDay = 3)
        {
            var tasks = new List<Task<IEnumerable<T>>>();
            foreach (var partitionKey in partitionTime.GetPartitionRange(partitionCheckDay))
            {
                var p = new DynamicParameters(parameters);
                p.Add("YYYYMM", partitionKey);
                tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: p));
            }

            Task.WhenAll(tasks);

            var result = new List<T>();
            foreach (var item in tasks)
            {
                result.AddRange(item.Result);
            }
            return result;
        }

        public int Execute(DateTime partitionTime, string spName, object parameters, IDbTransaction transaction = null)
        {
            var p = new DynamicParameters(parameters);
            p.Add("YYYYMM", partitionTime.GetPartitionKey());
            return _mainDbClient.QueryFirstAsync<int>(spName,p, transaction).Result;
        }

        public IEnumerable<IReadOnlyDictionary<string, object>> GetUnsettleDataList(string spName, TpUnsettle tpUnsettle, int partitionCheckDay = 3)
        {
            var tasks = new List<Task<IEnumerable<dynamic>>>();
            foreach (var partitionKey in tpUnsettle.PartitionTime.GetPartitionRange(partitionCheckDay))
            {
                tasks.Add(_mainDbClient.QueryAsync(spName, parameters: new { tpUnsettle.TransId, YYYYMM = partitionKey }));
            }

            Task.WhenAll(tasks);

            var result = new List<IReadOnlyDictionary<string, object>>();
            foreach (var item in tasks)
            {
                result.AddRange((IEnumerable<IReadOnlyDictionary<string, object>>)item.Result);
            }
            return result;
        }
    }
}
