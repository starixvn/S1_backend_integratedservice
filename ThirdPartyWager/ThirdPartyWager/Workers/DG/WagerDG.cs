using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerDG : BackgroundService
    {
        private readonly ILogger<WagerDG> logger;
        private readonly WagerDGBLL wagerDGBLL;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private Timer timer;
        private string logName = Tp.DG.ToString();

        public WagerDG(
            ILogger<WagerDG> logger, 
            IHostEnvironment environment, 
            WagerDGBLL wagerDGBLL,
            Log2File log2File
        )
        {
            this.logger = logger;
            this.wagerDGBLL = wagerDGBLL;
            this.environment = environment;
            this.log2File = log2File;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private async void DoTask(object state)
        {
            var isContinue = false;
            try
            {
                isContinue = await wagerDGBLL.GetWagersAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, logName);
                log2File.Error(ex.ToString(), logName);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (environment.IsDevelopment() || environment.IsEnvironment("QA") || isContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(10);
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
