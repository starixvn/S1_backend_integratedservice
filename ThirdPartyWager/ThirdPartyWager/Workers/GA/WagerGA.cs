using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerGA : BackgroundService
    {
        private readonly int _tpId = (int)Tp.GA;
        private readonly string _tpName = Tp.GA.ToString();
        private Timer _timer;
        private readonly WagerGABLL _wagerGABLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerGA> _logger;
        private readonly Dictionary<int, GAConfig> _siteConfigDic;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        public WagerGA(IServiceProvider provider)
        {
            _wagerGABLL = provider.GetRequiredService<WagerGABLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerGA>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GAConfig>(Tp.GA.ToString());
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
                var time = _dateTimeHelper.GetTimeByMinuteRevert(iRangMins, timeZone, _timeFormat, DateTime.UtcNow);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(3);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
