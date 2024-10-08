using System.Data;
using System.Diagnostics;

using Dapper;

using MySql.Data.MySqlClient;

using Serilog.Context;

using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.Service.Library
{
    public sealed class MySqlClient
    {
        public readonly DataBase DataBase;

        private readonly string _connectionString;

        private readonly IEnumerable<string> _backupConnectionStringList;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = null;
        }

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString, IEnumerable<string> backupConnectionStringList)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = backupConnectionStringList;
        }

        private string HealthCheck()
        {
            if (_backupConnectionStringList == null)
            {
                return _connectionString;
            }

            foreach (var backupConn in _backupConnectionStringList)
            {
                try
                {
                    // 設定相同連線不用檢查
                    if (_connectionString == backupConn)
                    {
                        return _connectionString;
                    }

                    using var connection = new MySqlConnection(backupConn);
                    var dbSlaveStatus = connection.QueryFirstOrDefault<DBSlaveStatus>("Service_HealthCheck_V2", commandType: CommandType.StoredProcedure);

                    // 拿不到Status或延遲太久都換下一個連線
                    if (dbSlaveStatus == null || dbSlaveStatus.Seconds_Behind_Master > 180)
                    {
                        _logger.LogWarning($"{ReplaceDBString(backupConn)} | DelayTime = {dbSlaveStatus?.Seconds_Behind_Master}");
                        continue;
                    }

                    return backupConn;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{ReplaceDBString(backupConn)} | HealthCheck Exception");
                }
            }

            // 前面連線檢查都有問題，用最後一個連線
            return _backupConnectionStringList.Last();
        }

        public MySqlConnection GetMySqlConnection()
        {
            var conn = HealthCheck();
            return new MySqlConnection(conn);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();

            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(sql,
                    parameters: parameters,
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, sql);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{sql} {sw.Elapsed:c}");
            return result.ToList();
        }

        public IEnumerable<dynamic> Query(string name, object parameters = null)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();

            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                result = connection.Query(name, parameters, commandType: CommandType.StoredProcedure);
                // Query 非泛型底層回傳行無不同，不無法使用
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();

            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);

                var command = new CommandDefinition(name, parameters,
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result.ToList();
        }

        public T QueryFirst<T>(string name, object parameters = null)
        {
            T result;
            var sw = Stopwatch.StartNew();

            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(name, parameters,
                    commandTimeout: null,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.QueryFirstOrDefault<T>(command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public dynamic QueryFirst(string name, object parameters = null)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();

            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                result = connection.QueryFirstOrDefault(name, parameters, commandType: CommandType.StoredProcedure);
                // QueryFirstOrDefault 非泛型底層回傳行無不同，不無法使用
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, object parameters = null)
        {
            int result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, IDbTransaction transaction, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                result = transaction.Connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        private string ReplaceDBString(string conn)
        {
            return conn.Substring(0, 28);
        }
    }
}

