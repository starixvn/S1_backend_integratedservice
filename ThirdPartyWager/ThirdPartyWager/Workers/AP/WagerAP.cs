using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerAP : BackgroundService
    {
        private readonly ILogger<WagerAP> _logger;
        private readonly WagerAPBLL _wagerAPBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer timerMini;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly int timeRange = 20; //in minutes 
        private readonly int quickTime = 45; // in seconds
        private readonly int runTime = 90; // in seconds
        private readonly string _tpName = Tp.AP.ToString();
        private readonly int _tpId = (int)Tp.AP;

        public WagerAP(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerAP>>();
            _wagerAPBLL = provider.GetRequiredService<WagerAPBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        _logger.LogError($"SiteId={site.SiteId}, no execute");
                        continue;
                    }

                    var siteId = site.SiteId;
                    var res = await _wagerAPBLL.ExecWagerWorker(siteId, tpType, timeRange);
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
