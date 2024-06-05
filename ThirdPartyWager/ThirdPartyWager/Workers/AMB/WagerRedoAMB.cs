using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoAMB : BackgroundService
    {
        private readonly string _tpName = Tp.AMB.ToString();
        private Timer _timer;
        private readonly WagerAMBBLL _wagerAMBBLL;
        private readonly ILogger<WagerGA> _logger;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 3; // in minutes
        private readonly int runTime = 1; // in hours
        private readonly int tpType = (int)WagerTimerType.RedoWager;

        public WagerRedoAMB(IServiceProvider provider)
        {
            _wagerAMBBLL = provider.GetRequiredService<WagerAMBBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerGA>>();
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
                quick = await _wagerAMBBLL.ExecWagerRedoWorker(tpType, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick)
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(quickTime);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromHours(runTime);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

    }
}