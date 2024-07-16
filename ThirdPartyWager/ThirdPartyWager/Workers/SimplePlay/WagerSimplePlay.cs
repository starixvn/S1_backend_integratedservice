using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSimplePlay : BackgroundService
    {
        private readonly ILogger<WagerSimplePlay> _logger;
        private readonly WagerSimplePlayBLL _wagerSimplePlayBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 20; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 90; // in seconds
        private readonly string _tpName = Tp.SimplePlay.ToString();

        public WagerSimplePlay(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSimplePlay>>();
            _wagerSimplePlayBLL = provider.GetRequiredService<WagerSimplePlayBLL>();
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
                quick = await _wagerSimplePlayBLL.ExecWagerWorker(tpType, timeRange);
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
