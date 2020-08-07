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
using ThirdPartySeamlessWallet.Service.DAL;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class BonusWorker : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;

        public BonusWorker(IConfiguration configuration, WalletService walletService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var bonusId in walletService.GetBonusFulfill())
            {

            }

            return Task.CompletedTask;
        }
    }
}
