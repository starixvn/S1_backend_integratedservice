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
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.DAL;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class BonusWorker : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly IHostEnvironment environment;
        private readonly ILogger<BonusWorker> logger;
        private Timer timer;
        public BonusWorker(IConfiguration configuration, IHostEnvironment environment, WalletService walletService, ILogger<BonusWorker> logger)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.environment = environment;
            this.logger = logger;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            try
            {
                foreach (var bonus in walletService.GetBonusFulfill())
                {
                    walletService.BonusSettle(bonus);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
