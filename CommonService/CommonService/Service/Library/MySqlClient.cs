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

    public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString)
    {
        LogContext.PushProperty("DataBase", dataBase.ToString());
        _connectionString = connectionString;
        _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
        DataBase = dataBase;
    }

    public MySqlConnection GetMySqlConnection()
    {
        return new MySqlConnection(_connectionString);
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

    public IEnumerable<T> Query<T>(string sql, object parameters)
    {
        IEnumerable<T> result;
        var sw = Stopwatch.StartNew();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            var command = new CommandDefinition(sql, parameters, // 使用 CommandDefinition 不讓 Dapper 用預設值建立
                commandType: CommandType.StoredProcedure,
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
}