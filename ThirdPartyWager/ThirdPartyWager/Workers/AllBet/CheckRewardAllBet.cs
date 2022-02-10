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
    public class CheckRewardAllBet : BackgroundService
    {
        private readonly ILogger<CheckRewardAllBet> _logger;
        private readonly RewardAllBetBLL _rewardAllBetBLL;

        private Timer timer;
        private readonly int tpType = 3;
        private readonly int timeRange = 20; //in minutes
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 180; // in seconds
        private readonly string _tpName = Tp.Allbet.ToString();

        public CheckRewardAllBet(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckRewardAllBet>>();
            _rewardAllBetBLL = provider.GetRequiredService<RewardAllBetBLL>();
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
            var quick = false;
            try
            {
                quick = await _rewardAllBetBLL.ExecRewardWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
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
