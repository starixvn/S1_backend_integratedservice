using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoSimplePlay : BackgroundService
    {
        private readonly ILogger<WagerRedoSimplePlay> _logger;
        private readonly WagerSimplePlayBLL _wagerSimplePlayBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 300; // in seconds
        private readonly int runTime = 1; // in hours
        private readonly string _tpName = Tp.SimplePlay.ToString();

        public WagerRedoSimplePlay(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoSimplePlay>>();
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
                quick = await _wagerSimplePlayBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromHours(runTime);
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
