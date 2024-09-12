using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSG : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerSG> _logger;
        private readonly WagerSGBLL _wagerSGBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _iRangMins = 20;
        private readonly int _tpId = (int)Tp.SG;
        private readonly string _tpName = Tp.SG.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;

        public WagerSG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSG>>();
            _wagerSGBLL = provider.GetRequiredService<WagerSGBLL>();
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
                            var res = _wagerSGBLL.ExecWagerWorker(siteId, _tpType, _iRangMins);
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
                var dueTimeSpan = quick ? TimeSpan.FromSeconds(35) : TimeSpan.FromSeconds(45);
                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        //private async void DoTask(object state)
        //{
        //    LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
        //    LogContext.PushProperty("Tp", _tpName);
        //    var quick = 0;

        //    try
        //    {
        //        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
        //        int last = _iRangMins - 1;
        //        DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : dataTpTimer.StartTime.AddSeconds(-1);

        //        if (timerTime < DateTime.Now)
        //        {
        //            var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, _timeFormat, timerTime);
        //            var result = await _wagerSGBLL.GetWagersAsync(_tpId, time.startTime, time.endTime);
        //            if (result)
        //            {
        //                _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
        //            }
        //        }

        //        var ts = DateTime.Now - timerTime;
        //        var diffMin = (int)ts.TotalMinutes;
        //        if (diffMin > 5)
        //        {
        //            quick = 1;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, ex.Message);
        //    }
        //    finally
        //    {
        //        var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(35) : TimeSpan.FromSeconds(45);
        //        _timer.Change(dueTimeSpan, TimeSpan.Zero);
        //    }
        //}

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
