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
    public class WagerRedoDG : BackgroundService
    {
        private readonly ILogger<WagerDG> logger;
        private readonly WagerRedoDGBLL wagerRedoDGBLL;

        public WagerRedoDG(ILogger<WagerDG> logger, WagerRedoDGBLL wagerRedoDGBLL)
        {
            this.logger = logger;
            this.wagerRedoDGBLL = wagerRedoDGBLL;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    DoTask();
            //    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(10000, stoppingToken);
            //}
        }

        private async void DoTask()
        {
            try
            {
                await wagerRedoDGBLL.RedoWagersAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

        }
    }
}
