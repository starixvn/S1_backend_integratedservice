using Microsoft.Extensions.DependencyInjection;
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
    public class CheckMega : BackgroundService
    {
        private readonly ILogger<CheckMega> _logger;
        private readonly WagerMegaBLL _wagerMegaBLL;

        private Timer timer;

        public CheckMega(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckMega>>();
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
            bool forContinue = false;

            try
            {
                forContinue = _wagerMegaBLL.CheckWagerAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(10);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
