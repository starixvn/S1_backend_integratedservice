using System.Data;
using System.Diagnostics;
using Dapper;
using MySql.Data.MySqlClient;
using Serilog.Context;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.Library
{
    public sealed class MySqlClient
    {
        public DataBase DataBase { get; set; }

        private readonly string _connectionString;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(string connectionString, ILogger<MySqlClient> logger)
        {
            LogContext.PushProperty("DataBase", DataBase.ToString());
            _connectionString = connectionString;
            _logger = logger;
        }

        public MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        #region Query

        public async Task<T> QueryFirstAsync<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryFirstOrDefaultAsync<T>(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
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

        public T QueryFirst<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters, 
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.QueryFirstOrDefault<T>(command);
                connection.Close();
                connection.Dispose();
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
        
        public IEnumerable<T> Query<T>(string name)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
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

        public IEnumerable<T> Query<T>(string name, object parameters)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name,parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
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

        public T Query<T>(string name, SqlMapper.IDynamicParameters parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name,parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                var multiple = connection.QueryMultiple(command);
                result = func.Invoke(multiple);
                connection.Close();
                connection.Dispose();
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
        
        public async Task<IEnumerable<T>> QueryAsync<T>(string name, object parameters)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryAsync<T>(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
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

        public async Task<T> QueryAsync<T>(string name, SqlMapper.IDynamicParameters parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var multiple =
                    connection.QueryMultipleAsync(name, parameters, commandType: CommandType.StoredProcedure);
                result = func.Invoke(await multiple);
                await connection.CloseAsync();
                await connection.DisposeAsync();
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

        #region Execute

        public bool Execute(string name, SqlMapper.IDynamicParameters parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
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

        public int Execute(string name, object parameters)
        {
            int result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
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

        public int Execute(string command)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(command, CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, command);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug(command);
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

        public async Task<int> ExecuteAsync(string name, object parameters)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
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
        
      }
}