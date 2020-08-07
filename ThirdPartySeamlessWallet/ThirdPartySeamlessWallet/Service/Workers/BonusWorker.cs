using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class BonusWorker : BackgroundService
    {
        private readonly IConfiguration configuration;

        public BonusWorker(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            conn.Execute(@"Test_Scott", new { Bonus = 0 }, transaction: trans, commandType: CommandType.StoredProcedure);

            trans.Commit();
            return Task.CompletedTask;
        }
    }
}
