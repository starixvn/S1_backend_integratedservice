using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Library
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

        public IEnumerable<T> Query<T>(string spName, CommandType commandType = CommandType.StoredProcedure, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Query<T>(spName, parameters, commandTimeout: commandTimeout, commandType: commandType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, spName);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{spName} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, CommandType commandType = CommandType.StoredProcedure, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            var result = 0;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: commandType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
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
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public T QueryFirst<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.QueryFirstOrDefault<T>(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string spName, CommandType commandType = CommandType.StoredProcedure, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryAsync<T>(spName, parameters, commandTimeout: commandTimeout, commandType: commandType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, spName);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{spName} {sw.Elapsed:c}");
            return result;
        }

        public async Task<IEnumerable<dynamic>> QueryAsync(string spName, CommandType commandType = CommandType.StoredProcedure, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<dynamic> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryAsync(spName, parameters, commandTimeout: commandTimeout, commandType: commandType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, spName);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{spName} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryFirstAsync<T>(string name, IDbTransaction transaction, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            T result;
            try
            {
                result = await transaction.Connection.QueryFirstAsync<T>(name, parameters, transaction, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }
    }
}