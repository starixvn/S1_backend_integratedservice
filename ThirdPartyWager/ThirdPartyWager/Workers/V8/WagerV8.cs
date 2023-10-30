using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerV8 : BackgroundService
    {
        private readonly ILogger<WagerV8> _logger;
        private readonly WagerV8BLL _wagerV8BLL;
        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 20; //in minutes
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 120;  // in seconds
        private readonly string _tpName = Tp.V8.ToString();

        public WagerV8(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerV8>>();
            _wagerV8BLL = provider.GetRequiredService<WagerV8BLL>();
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
                quick = await _wagerV8BLL.ExecWagerWorker(tpType, timeRange);
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