using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerWM : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerWM> _logger;
        private readonly WagerWMBLL _wagerWMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _intervalMins = 3;
        private readonly int _iRangMins = 20;
        private readonly int _tpId = (int)Tp.WM;
        private readonly string _tpName = Tp.WM.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;

        public WagerWM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerWM>>();
            _wagerWMBLL = provider.GetRequiredService<WagerWMBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoTask(object state)
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
                            var res = _wagerWMBLL.ExecWagerWorker(siteId, _tpType, _iRangMins);
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
                var dueTimeSpan = (quick) ? TimeSpan.FromSeconds(90) : TimeSpan.FromMinutes(_intervalMins);
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
