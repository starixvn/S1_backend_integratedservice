using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class CheckRefundAG : BackgroundService
    {
        private readonly ILogger<CheckRefundAG> _logger;
        private readonly IHostEnvironment _environment;
        private readonly TicketAGBLL _ticketAGBLL;
        private Timer timer;

        public CheckRefundAG(IServiceProvider provider, IHostEnvironment environment)
        {
            LogContext.PushProperty("Tp", Tp.AG);
            _logger = provider.GetRequiredService<ILogger<CheckRefundAG>>();
            _ticketAGBLL = provider.GetRequiredService<TicketAGBLL>();
            _environment = environment;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            if (!_environment.IsProduction())
            {
                return;
            }
            try
            {
                await _ticketAGBLL.ExecRefundWorker();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
