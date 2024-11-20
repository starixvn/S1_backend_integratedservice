using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoSA : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerRedoSA> _logger;
        private readonly WagerSABLL _wagerSABLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _timeRange = 60;//in minutes
        private readonly int _quickTime = 120; // in seconds
        private readonly int _runTime = 60; // in minutes
        private readonly int _tpId = (int)Tp.SA;
        private readonly string _tpName = Tp.SA.ToString();
        private readonly int _tpType = (int)WagerTimerType.RedoWager;

        public WagerRedoSA(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoSA>>();
            _wagerSABLL = provider.GetRequiredService<WagerSABLL>();
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
            var failed = 0;

            try
            {
                var tasks = new List<Task>();
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var siteId = site.SiteId;
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var res = _wagerSABLL.ExecWagerRedoWorker(siteId, _tpType, _timeRange);
                            if (res.Result == true)
                            {
                                quick = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            Interlocked.Increment(ref failed);
                        }
                    }));
                }
                var t = Task.WhenAll(tasks);
                t.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick) ? TimeSpan.FromSeconds(_quickTime) : TimeSpan.FromMinutes(_runTime);
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
