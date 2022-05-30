using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL.YGG;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.YGG
{
    public class WagerYGG: BackgroundService
    {
        private readonly ILogger<WagerYGG> _logger;
        private readonly WagerYGGBLL _wagerYGGBLL;

        private Timer _timer;
        private readonly int tpType = 1;
        private readonly int timeRange = 20; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 180; // in seconds
        private readonly string _tpName = Tp.YGG.ToString();

        public WagerYGG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerYGG>>();
            _wagerYGGBLL = provider.GetRequiredService<WagerYGGBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            var quick = false;

            try
            {
               // quick = await _wagerEVOBLL.ExecWagerWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

    }
}
