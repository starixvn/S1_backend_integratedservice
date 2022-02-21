using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;

namespace ThirdPartyWager.Workers
{
    public class WagerMega : BackgroundService
    {

        private readonly ILogger<WagerMega> _logger;
        private readonly WagerMegaBLL _wagerMegaBLL;

        private Timer timer;

        public WagerMega(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMega>>();
            _wagerMegaBLL = provider.GetRequiredService<WagerMegaBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            bool isError = false;

            try
            {
                var result = _wagerMegaBLL.GetWagerAsync();
                if (!result)
                {
                    isError = true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(10);

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

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
