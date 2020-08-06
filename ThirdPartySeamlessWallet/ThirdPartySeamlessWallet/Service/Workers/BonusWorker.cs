using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
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
            return Task.CompletedTask;
        }
    }
}
