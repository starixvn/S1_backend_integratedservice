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
    public class WagerRedoEVO : BackgroundService
    {
        private readonly ILogger<WagerRedoEVO> _logger;
        private readonly WagerEVOBLL _wagerEVOBLL;

        private Timer timer;
        private readonly int tpType = 2;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 2; // in hours

        public WagerRedoEVO(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoEVO>>();
            _wagerEVOBLL = provider.GetRequiredService<WagerEVOBLL>();
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
                quick = await _wagerEVOBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromHours(runTime);
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
