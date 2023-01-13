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
            MySqlConnection connection = null;
            
            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(name,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
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
            _logger.LogDebug(name);
            return result;
        }

        public IEnumerable<T> Query<T>(string name, SqlMapper.IDynamicParameters parameters, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();
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

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, SqlMapper.IDynamicParameters parameters, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;
            
            try
            {
                var connectionString = $"{_connectionString}ProcedureCacheSize=0;";
                connection = new MySqlConnection(connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

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
        
        public T Query<T>(string name, SqlMapper.IDynamicParameters parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(name, parameters,
                    commandTimeout: null,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                var multiple = connection.QueryMultiple(command);
                result = func.Invoke(multiple);
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

        public async Task<IEnumerable<T>> QueryAsync<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(name, parameters,
                    commandTimeout: null,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = await connection.QueryAsync<T>(command);
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
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryAsync<T>(string name, SqlMapper.IDynamicParameters parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(name, parameters,
                    commandTimeout: null,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                var multiple = connection.QueryMultipleAsync(command);
                result = func.Invoke(await multiple);
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
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryFirstAsync<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(name, parameters,
                    commandTimeout: null,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = await connection.QueryFirstOrDefaultAsync<T>(command);
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
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public T QueryFirst<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;
            
            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

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
        
        public IEnumerable<T> Query<T>(string sql, object parameters, CommandType commandType = CommandType.StoredProcedure)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;
            
            try
            {
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();

                var command = new CommandDefinition(sql, parameters,
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
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{sql} {sw.Elapsed:c}");
            return result;
        }

        
        public dynamic QueryFirst(string name, object parameters)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                //  這個暫時不沒辦法動，要改底層
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();
                result = connection.QueryFirstOrDefault(name, parameters, commandType: CommandType.StoredProcedure);
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

        public IEnumerable<dynamic> Query(string name, object parameters)
        {
            dynamic result;
            var sw = Stopwatch.StartNew();
            MySqlConnection connection = null;

            try
            {
                //  這個暫時不沒辦法動，要改底層
                connection = new MySqlConnection(_connectionString);
                if (connection.State != ConnectionState.Open) connection.Open();
                result = connection.Query(name, parameters, commandType: CommandType.StoredProcedure);
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

