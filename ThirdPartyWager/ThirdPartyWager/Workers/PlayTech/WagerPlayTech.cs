using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerPlayTech : BackgroundService
    {
        private readonly ILogger<WagerPlayTech> _logger;
        private readonly WagerPlayTechBLL _wagerPlayTechBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 15; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 90; // in seconds
        private readonly string _tpName = Tp.PlayTech.ToString();
        private readonly int _tpId = (int)Tp.PlayTech;

        public WagerPlayTech(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerPlayTech>>();
            _wagerPlayTechBLL = provider.GetRequiredService<WagerPlayTechBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteData in siteList)
                {
                    var res = await _wagerPlayTechBLL.ExecWagerWorker(siteData, tpType, timeRange);
                    if (res)
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
                timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
