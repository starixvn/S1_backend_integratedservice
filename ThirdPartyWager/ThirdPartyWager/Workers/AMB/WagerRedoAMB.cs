using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoAMB : BackgroundService
    {
        private Timer _timer;
        private readonly string _tpName = Tp.AMB.ToString();
        private readonly WagerAMBBLL _wagerAMBBLL;
        private readonly ILogger<WagerRedoAMB> _logger;
        private readonly int _timeRange = 9; //in minutes
        private readonly int _quickTime = 2; // in minutes
        private readonly int _runTime = 5; // in minutes
        private readonly int _tpType = (int)WagerTimerType.RedoWager;

        public WagerRedoAMB(IServiceProvider provider)
        {
            _wagerAMBBLL = provider.GetRequiredService<WagerAMBBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoAMB>>();
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
                quick = await _wagerAMBBLL.ExecWagerRedoWorker(_tpType, _timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromMinutes(_quickTime) : TimeSpan.FromMinutes(_runTime);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

    }
}