using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMegaService
    {
        private readonly IConfiguration configuration;

        public WagerMegaService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(ReportMega bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerMega_Ins", bet, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<WagerMegaTotal> GetWagerTotal(long beginTime, long endTime, string currency)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<WagerMegaTotal>("Service_TpWagerMega_Total_Get", new { BeginTime = beginTime, EndTime = endTime, Currency = currency }, commandType: CommandType.StoredProcedure);
        }
    }
}
