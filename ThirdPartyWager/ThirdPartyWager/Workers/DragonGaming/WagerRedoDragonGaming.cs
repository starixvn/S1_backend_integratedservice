using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoDragonGaming : BackgroundService
    {
        private readonly int _tpId = (int)Tp.DragonGaming;
        private readonly string _tpName = Tp.DragonGaming.ToString();
        private readonly int _workerType = (int)WagerTimerType.RedoWager;
        private readonly string _workerTypeName = WagerTimerType.RedoWager.ToString();

        private readonly WagerDragonGamingBLL _wagerDragonGamingBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly Dictionary<int, DragonGamingConfig> _siteConfigDic;
        private readonly ILogger<WagerRedoDragonGaming> _logger;

        private Timer _timer;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 15;
        private readonly int intervalMins = 5;

        public WagerRedoDragonGaming(IServiceProvider provider)
        {
            _wagerDragonGamingBLL = provider.GetRequiredService<WagerDragonGamingBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<DragonGamingConfig> (_tpName);
            _logger = provider.GetRequiredService<ILogger<WagerRedoDragonGaming>>();
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
            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _workerType);
                var nowDateTime = DateTime.Now;

                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = _tpId,
                        Type = _workerType,
                        StartTime = nowDateTime.AddHours(-2).AddMinutes(-15),
                    };
                }
                else
                {
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (dataTpTimer.StartTime <= nowDateTime.AddHours(-2).AddMinutes(-15))
                {
                    var time = _dateTimeHelper.GetTimeByMinute(iRangMins - 1, timeZone, "yyyy-MM-dd HH:mm:ss", dataTpTimer.StartTime);
                    var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                    foreach (var siteInfo in siteList)
                    {
                        var _setting = _siteConfigDic[siteInfo.SiteId];
                        if (_setting == null) continue;

                        await _wagerDragonGamingBLL.GetWagerAsync(_setting, time.startTime.ToString(), time.endTime.ToString(), _workerTypeName.ToString());
                    }
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _workerType, time.endUnix);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{_tpName}_{_workerTypeName}:{ex.Message}");
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
