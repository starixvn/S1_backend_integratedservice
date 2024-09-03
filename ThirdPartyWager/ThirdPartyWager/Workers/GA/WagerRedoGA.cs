using Serilog.Context;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoGA : BackgroundService
    {
        private readonly int _tpId = (int)Tp.GA;
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private readonly string _tpName = Tp.GA.ToString();
        private Timer _timer;
        private readonly Dictionary<int, GAConfig> _siteConfigDic;
        private readonly WagerGABLL _wagerGABLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerRedoGA> _logger;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 30;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int intervalMins = 1;

        public WagerRedoGA(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GAConfig>(Tp.GA.ToString());
            _wagerGABLL = provider.GetRequiredService<WagerGABLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoGA>>();
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
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                var nowDateTime = DateTime.Now;

                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = _tpId,
                        Type = _tpType,
                        StartTime = nowDateTime.AddHours(-2),
                    };
                }
                else
                {
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (dataTpTimer.StartTime <= nowDateTime.AddMinutes(-30))
                {
                    var time = _dateTimeHelper.GetTimeByMinute(iRangMins - 1, timeZone, _timeFormat, dataTpTimer.StartTime);

                    var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                    foreach (var site in siteList)
                    {
                        if (site.TicketKey == 0)
                        {
                            _logger.LogError($"SiteId={site.SiteId}, no execute");
                            continue;
                        }

                        var _setting = _siteConfigDic[site.SiteId];

                        if (_setting == null) continue;

                        await _wagerGABLL.GetWagerAsync(_setting, time.startTime, time.endTime);
                    }
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
