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
    public class WagerRedoSA : BackgroundService
    {
        private readonly ILogger<WagerRedoSA> logger;
        private readonly WagerRedoSABLL wagerRedoSABLL;
        private readonly IHostEnvironment environment;
        private Timer timer;

        public WagerRedoSA(ILogger<WagerRedoSA> logger, IHostEnvironment environment, WagerRedoSABLL wagerRedoSABLL)
        {
            this.logger = logger;
            this.wagerRedoSABLL = wagerRedoSABLL;
            this.environment = environment;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            try
            {
                await wagerRedoSABLL.RedoWagersAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(60);
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
