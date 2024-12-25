using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSimplePlay : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerSimplePlay> _logger;
        private readonly WagerSimplePlayBLL _wagerSimplePlayBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly int _timeRange = 20; //in minutes 
        private readonly int _quickTime = 45; // in seconds
        private readonly int _runTime = 90; // in seconds
        private readonly int _tpId = (int)Tp.SimplePlay;
        private readonly string _tpName = Tp.SimplePlay.ToString();

        public WagerSimplePlay(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSimplePlay>>();
            _wagerSimplePlayBLL = provider.GetRequiredService<WagerSimplePlayBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId).Where(x => x.TicketKey == 1);
                foreach (var site in siteList)
                {
                    var siteId = site.SiteId;
                    var res = await _wagerSimplePlayBLL.ExecWagerWorker(siteId, _tpType, _timeRange);
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
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(_quickTime) : TimeSpan.FromSeconds(_runTime);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
