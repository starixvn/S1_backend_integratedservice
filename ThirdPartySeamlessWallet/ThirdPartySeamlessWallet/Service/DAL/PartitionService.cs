using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Library;
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

        public IEnumerable<T> Query<T>(DateTime PartitionTime, string spName, object parameters, int partitionCheckDay = 3)
        {
            var tasks = new List<Task<IEnumerable<T>>>();
            var p1 = new DynamicParameters(parameters);
            p1.Add("YYYYMM", PartitionTime.ToString("yyyyMM"));
            tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: p1));

            if (PartitionTime.Day < partitionCheckDay)
            {
                var p2 = new DynamicParameters(parameters);
                p2.Add("YYYYMM", PartitionTime.AddMonths(-1).ToString("yyyyMM"));
                tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: p2));
            }

            Task.WhenAll(tasks);

            var result = new List<T>();
            foreach (var item in tasks)
            {
                result.AddRange(item.Result);
            }
            return result;
        }

        public IEnumerable<IReadOnlyDictionary<string, object>> Query(DateTime PartitionTime, string spName, object parameters, int partitionCheckDay = 3)
        {
            var tasks = new List<Task<IEnumerable<dynamic>>>();
            var p1 = new DynamicParameters(parameters);
            p1.Add("YYYYMM", PartitionTime.ToString("yyyyMM"));
            tasks.Add(_mainDbClient.QueryAsync(spName, parameters: p1));

            if (PartitionTime.Day < partitionCheckDay)
            {
                var p2 = new DynamicParameters(parameters);
                p2.Add("YYYYMM", PartitionTime.AddMonths(-1).ToString("yyyyMM"));
                tasks.Add(_mainDbClient.QueryAsync(spName, parameters: p2));
            }

            Task.WhenAll(tasks);

            var result = new List<IReadOnlyDictionary<string, object>>();
            foreach (var item in tasks)
            {
                result.AddRange((IEnumerable<IReadOnlyDictionary<string, object>>)item.Result);
            }
            return result;
        }

        public int Execute(DateTime PartitionTime, string spName, object parameters, IDbTransaction transaction = null)
        {
            var p = new DynamicParameters(parameters);
            p.Add("YYYYMM", PartitionTime.ToString("yyyyMM"));
            return _mainDbClient.ExecuteAsync(spName, transaction: transaction, parameters: p).Result;
        }


    }
}
