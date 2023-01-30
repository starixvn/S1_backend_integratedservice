using System.Data;
using System.Diagnostics;
using Dapper;
using MySql.Data.MySqlClient;
using Serilog.Context;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.Service.Library
{
    public sealed class MySqlClient
    {
        public DataBase DataBase { get; set; }
        private readonly string _connectionString;
        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(string connectionString, ILogger<MySqlClient> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        #region Query

        public IEnumerable<T> Query<T>(string name)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                var command = new CommandDefinition(name,
                    parameters:null, 
                    commandTimeout: null,
                    commandType: CommandType.Text,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug(name);
            return result.ToList();
        }

        public IEnumerable<T> Query<T>(string sql, object parameters, CommandType commandType = CommandType.StoredProcedure)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(sql,
                    parameters: parameters,
                    commandTimeout: null,
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

        public IEnumerable<dynamic> Query(string name, object parameters)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
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

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, SqlMapper.IDynamicParameters parameters, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();

            try
            {
                var connectionString = $"{_connectionString}ProcedureCacheSize=0;";
                using var connection = new MySqlConnection(connectionString);
                
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

        public T QueryFirst<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();

            try
            {
                using var connection = new MySqlConnection(_connectionString);

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

        public dynamic QueryFirst(string name, object parameters)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
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

        #endregion

        #region Excute

        public int Execute(string name, object parameters)
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

        public bool Execute(string command)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(command, CommandType.Text);
            }
            catch (Exception e)
            {
                _logger.LogError(e, command);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug(command);
            return true;
        }

        public async Task<bool> ExecuteAsync(string name, SqlMapper.IDynamicParameters parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return true;
        }

        #endregion

    }
}

