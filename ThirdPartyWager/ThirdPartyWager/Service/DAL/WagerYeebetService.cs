using Dapper;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;
using System.Text.Json;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL;
public class WagerYeebetService
{
    private readonly IConfiguration configuration;

    public WagerYeebetService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public Task<int> InsertWager(WagerYeebetModel data)
    {
        using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
        return conn.ExecuteAsync("Service_TpWagerLC_Yeebet_Ins", new DynamicParameters(data), commandType: CommandType.StoredProcedure);
    }
}
