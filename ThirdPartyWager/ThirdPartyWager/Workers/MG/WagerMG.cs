using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.MG;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.Workers.MG
{
    public class WagerMG : BackgroundService
    {
        private readonly ILogger<WagerMG> _logger;
        private readonly IMGBLL _mgBll;
        private Timer _timer;
        

        public WagerMG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMG>>();
            _mgBll = provider.GetRequiredService<IMGBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object? state)
        {
            try
            {
                await _mgBll.ReSettle();
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerMG.DoTask.Exception");
            }
            finally
            {
                _timer.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}