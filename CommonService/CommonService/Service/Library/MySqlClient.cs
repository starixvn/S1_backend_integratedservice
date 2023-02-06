using System.Data;
using System.Diagnostics;
using CommonService.Service.Model.enums;
using Dapper;
using MySql.Data.MySqlClient;
using Serilog.Context;

namespace CommonService.Service.Library;

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
        
        public async Task<T> QueryFirstAsync<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();

            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                
                result = await connection.QueryFirstOrDefaultAsync<T>(command);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
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
                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
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
                var command = new CommandDefinition(name, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
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
                var command = new CommandDefinition(sql, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
                    commandType: commandType,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
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

        public T Query<T>(string name, object parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public IEnumerable<T> Query<T>(string sql, IDbTransaction transaction, object parameters)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                result = transaction.Connection.Query<T>(sql, param: parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, sql);
                throw;
            }
            
            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{sql} {sw.Elapsed:c}");
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryAsync<T>(string name, object parameters, Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                
                var multiple = connection.QueryMultipleAsync(command);
                result = func.Invoke(await multiple);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        #endregion
        
        #region Execute
        
        public int Execute(string name, object parameters)
        {
            var result = 0;
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<int> ExecuteAsync(string name, IDbTransaction transaction, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                result = await transaction.Connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string command)
        {
            int result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(command, CommandType.Text);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, command);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug(command);
            return result;
        }

        public async Task<int> ExecuteAsync(string name, object parameters)
        {
            int result;
            var sw = Stopwatch.StartNew();
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

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        public T ExecuteScalar<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.ExecuteScalar<T>(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        public T ExecuteScalar<T>(string name, IDbTransaction transaction, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                result = transaction.Connection.ExecuteScalar<T>(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        public async Task<T> ExecuteScalarAsync<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.ExecuteScalarAsync<T>(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        #endregion
    }