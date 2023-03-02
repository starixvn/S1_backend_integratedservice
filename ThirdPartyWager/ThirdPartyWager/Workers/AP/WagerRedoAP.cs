using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoAP : BackgroundService
    {
        private readonly ILogger<WagerRedoAP> _logger;
        private readonly WagerAPBLL _wagerAPBLL;
        private readonly CommonBLL _commonBLL;

        private Timer timerMini;
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 60; //in minutes 
        private readonly int quickTime = 300; // in seconds
        private readonly int runTime = 1; // in hours
        private readonly string _tpName = Tp.AP.ToString();

        public WagerRedoAP(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoAP>>();
            _wagerAPBLL = provider.GetRequiredService<WagerAPBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerMini = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = false;
            try
            {
                var siteList = _commonBLL.GetSiteList();

                foreach (var site in siteList)
                {
                    if (site.SiteId == 4)
                    {
                        continue;
                    }

                    var siteId = site.SiteId;
                    var res = await _wagerAPBLL.ExecWagerRedoWorker(siteId, tpType, timeRange);
                    if (res == true)
                    {
                        quick = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                timerMini?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerMini?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
