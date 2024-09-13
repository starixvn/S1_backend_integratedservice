using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerDragonGaming : BackgroundService
    {
        private readonly int _tpId = (int)Tp.DragonGaming;
        private readonly string _tpName = Tp.DragonGaming.ToString();
        private readonly int _workerType = (int)WagerTimerType.Wager;
        private readonly string _workerTypeName = WagerTimerType.Wager.ToString();

        private readonly WagerDragonGamingBLL _wagerDragonGamingBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly Dictionary<int, DragonGamingConfig> _siteConfigDic;
        private readonly ILogger<WagerDragonGaming> _logger;

        private Timer _timer;
        private bool isFast = false;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 15;
        private readonly string timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerDragonGaming(IServiceProvider provider)
        {
            _wagerDragonGamingBLL = provider.GetRequiredService<WagerDragonGamingBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<DragonGamingConfig>(_tpName);
            _logger = provider.GetRequiredService<ILogger<WagerDragonGaming>>();
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
            var nowTime = DateTime.Now;
            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _workerType);

                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;

                var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, timeFormat, timerTime);

                isFast = false;
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                foreach (var siteInfo in siteList)
                {
                    var _setting = _siteConfigDic[siteInfo.SiteId];
                    if (_setting == null) continue;

                    await _wagerDragonGamingBLL.GetWagerAsync(_setting, time.startTime.ToString(), time.endTime.ToString(), _workerTypeName.ToString());
                }

                _thirdPartyBLL.SetThirdPartyTimer(_tpId, _workerType, time.endUnix);

                isFast = CheckQuick(dataTpTimer != null ? dataTpTimer.StartTime : DateTime.Now.AddMinutes(-10), 20);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_tpName}_{_workerTypeName}:{ex.Message}");
            }
            finally
            {
                var dueTimeSpan = isFast ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(60);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private bool CheckQuick(DateTime workTime, int delayTime)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > delayTime;
        }
    }
}
