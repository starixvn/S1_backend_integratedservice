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
    public class CheckTicketAG : BackgroundService
    {
        private readonly ILogger<CheckTicketAG> logger;
        private readonly TicketAGBLL ticketAGBLL;
        private readonly IHostEnvironment environment;
        private Timer timer;

        public CheckTicketAG(ILogger<CheckTicketAG> logger, IHostEnvironment environment, TicketAGBLL ticketAGBLL)
        {
            this.logger = logger;
            this.ticketAGBLL = ticketAGBLL;
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
                await ticketAGBLL.CheckTicketAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(5);

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
