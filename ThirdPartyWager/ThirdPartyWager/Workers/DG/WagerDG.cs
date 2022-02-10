using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerDG : BackgroundService
    {
        private readonly ILogger<WagerDG> logger;
        private readonly WagerDGBLL wagerDGBLL;
        private readonly IHostEnvironment environment;
        private Timer timer;
        private readonly string _tpName = Tp.DG.ToString();

        public WagerDG(
            ILogger<WagerDG> logger, 
            IHostEnvironment environment, 
            WagerDGBLL wagerDGBLL
        )
        {
            this.logger = logger;
            this.wagerDGBLL = wagerDGBLL;
            this.environment = environment;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var isContinue = false;
            try
            {
                isContinue = await wagerDGBLL.GetWagersAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
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
