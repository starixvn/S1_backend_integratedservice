using System.Data;

using Dapper;

using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class PartitionService
    {
        private readonly MySqlClient _mainDbClient;

        public PartitionService(IServiceProvider provider)
        {
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

        public int Execute(DateTime partitionTime, string spName, object parameters, IDbTransaction transaction)
        {
            var p = new DynamicParameters(parameters);
            p.Add("YYYYMM", partitionTime.GetPartitionKey());
            return _mainDbClient.QueryFirst<int>(spName, transaction, p);
        }

        public int Execute(DateTime partitionTime, string spName, object parameters) {
            var p = new DynamicParameters(parameters);
            p.Add("YYYYMM", partitionTime.GetPartitionKey());
            return _mainDbClient.QueryFirst<int>(spName, p);
        }

        public IEnumerable<T> GetUnsettleDataList<T>(string spName, TpUnsettle tpUnsettle, int partitionCheckDay = 3)
        {
            var tasks = new List<Task<IEnumerable<T>>>();
            foreach (var partitionKey in tpUnsettle.PartitionTime.GetPartitionRange(partitionCheckDay))
            {
                tasks.Add(_mainDbClient.QueryAsync<T>(spName, parameters: new { tpUnsettle.TransId, YYYYMM = partitionKey }));
            }

            Task.WhenAll(tasks);

            var result = new List<T>();
            foreach (var item in tasks)
            {
                result.AddRange(item.Result);
            }
            return result;
        }

        public IEnumerable<TResult> MonthlyQuery<TResult>(DateTime StartDate, DateTime endDate, string spName, object parameters)
        {
            var tasks = new List<Task<IEnumerable<TResult>>>();
            foreach (var partitionKey in StartDate.GetMonthlyPartitionRange(endDate))
            {
                var p = new DynamicParameters(parameters);
                p.Add("YYYYMM", partitionKey);
                var task = _mainDbClient.QueryAsync<TResult>(spName, parameters: p);
                tasks.Add(task);
            }

            Task.WhenAll(tasks);

            var result = Enumerable.Empty<TResult>();
            foreach (var item in tasks)
            {
                result = result.Concat(item.Result);
            }
            return result;
        }
    }
}
