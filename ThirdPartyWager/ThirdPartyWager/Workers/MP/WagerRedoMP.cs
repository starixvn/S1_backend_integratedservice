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
    public class WagerRedoMP : BackgroundService
    {
        private readonly ILogger<WagerRedoMP> _logger;
        private readonly WagerMPBLL _wagerMPBLL;

        private Timer timer;
        private readonly int tpType = 2;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 73; // in seconds
        private readonly int runTime = 3697; // in seconds

        public WagerRedoMP(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoMP>>();
            _wagerMPBLL = provider.GetRequiredService<WagerMPBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            bool quick = false;
            try
            {
                quick = await _wagerMPBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
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
