using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoWM : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerRedoWM> _logger;
        private readonly WagerWMBLL _wagerWMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _intervalMins = 60;
        private readonly int _tpId = (int)Tp.WM;
        private readonly string _tpName = Tp.WM.ToString();
        private readonly int _tpType = (int)WagerTimerType.RedoWager;

        public WagerRedoWM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoWM>>();
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
                            var res = _wagerWMBLL.ExecWagerRedoWorker(siteId, _tpType, _intervalMins);
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
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(120) : TimeSpan.FromMinutes(28);
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