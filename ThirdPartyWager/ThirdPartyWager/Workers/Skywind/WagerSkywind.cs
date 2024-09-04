using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSkywind : BackgroundService
    {
        private readonly ILogger<WagerSkywind> _logger;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerSkywindBLL _wagerSkywindBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer timer;
        private readonly int intervalMins = 1;
        private readonly int iRangMins = 10;
        private readonly int tpId = (int)Tp.Skywind;
        private readonly string _tpName = Tp.Skywind.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = 0;

        public WagerSkywind(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSkywind>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerSkywindBLL = provider.GetRequiredService<WagerSkywindBLL>();
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
            try
            {
                var SetTimer = true;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(-1);
                    last = iRangMins - 1;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ss.fffZ", timerTime);
                    var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
                    foreach (var siteData in siteList)
                    {
                        if (siteData.TicketKey == 0)
                        {
                            _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                            continue;
                        }

                        var result = await _wagerSkywindBLL.GetWagersAsync(siteData.SiteId, time.startTime, time.endTime);
                        if (!result)
                        {
                            SetTimer = false;
                        }
                    }
                    if (SetTimer)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 10)
                {
                    quick = 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
    }
}
