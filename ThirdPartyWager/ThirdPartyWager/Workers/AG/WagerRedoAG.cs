using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoAG : BackgroundService
    {
        private readonly ILogger<WagerRedoAG> _logger;
        private readonly WagerAGBLL _wagerAGBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IHostEnvironment _env;
        private Timer _timer;
        private readonly int _timeRange = 60; //in minutes 
        private readonly int _quickTime = 120; // in seconds
        private readonly int _runTime = 1; // in hours
        private readonly int _tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int _tpType = (int)WagerTimerType.RedoWager;


        public WagerRedoAG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoAG>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerAGBLL = provider.GetRequiredService<WagerAGBLL>();
            _env = provider.GetRequiredService<IWebHostEnvironment>();
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

            if (!_env.IsProduction())
            {
                return;
            }

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
                            var res = _wagerAGBLL.ExecWagerRedoWorker(siteId, _tpType, _timeRange);
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
                var dueTimeSpan = (quick) ? TimeSpan.FromSeconds(_quickTime) : TimeSpan.FromHours(_runTime);
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
