using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;

namespace ThirdPartyWager.Workers
{
    public class WagerDG : BackgroundService
    {
        private readonly ILogger<WagerDG> logger;
        private readonly WagerDGBLL wagerDGBLL;

        public WagerDG(ILogger<WagerDG> logger, WagerDGBLL wagerDGBLL)
        {
            this.logger = logger;
            this.wagerDGBLL = wagerDGBLL;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                DoTask();
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000, stoppingToken);
            }
        }
        
        private async void DoTask()
        {
           try
           {
                await wagerDGBLL.GetWagersAsync();
           }
           catch (Exception ex)
           {
               logger.LogError(ex.Message);
           }

        }

    }
}
