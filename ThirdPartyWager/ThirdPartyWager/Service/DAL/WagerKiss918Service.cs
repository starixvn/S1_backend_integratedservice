using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using ThirdPartyWager.Service.Model;
using System.Text.Json;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKiss918Service
    {
        private readonly IConfiguration configuration;

        public WagerKiss918Service(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<IEnumerable<WagerKiss918Model>> GetWager(string referenceID)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return await conn.QueryAsync<WagerKiss918Model>("Service_TpWagerKiss918_Detail_Get", new { _referenceID = referenceID }, commandType: CommandType.StoredProcedure);
        }

        public int InsertWager(IEnumerable<WagerKiss918Model> datas)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerKiss918Batch_Ins", new { _Infos = JsonSerializer.Serialize(datas) }, commandType: CommandType.StoredProcedure);
        }
    }
}
