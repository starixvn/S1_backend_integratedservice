using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerPegasus : BackgroundService
    {
        private readonly ILogger<WagerPegasus> _logger;
        private readonly WagerPegasusBLL _wagerPegasusBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly CommonBLL _commonBll;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer _timer;
        private readonly int _intervalMins = 3;
        private readonly int _iRangMins = 5;
        private readonly int _tpId = (int)Tp.Pegasus;
        private readonly string _tpName = Tp.Pegasus.ToString();
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly int _timeZone = 0;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;

        public WagerPegasus(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerPegasus>>();
            _wagerPegasusBll = provider.GetRequiredService<WagerPegasusBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(_tpName);
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
            var quick = 0;

            try
            {
                var siteList = _thirdPartyBll.GetTpListByTpId(_tpId);
                var TpCurrencyList = _commonBll.GetTpCurrencyList(_tpId);

                foreach (var siteInfo in siteList)
                {
                    if (siteInfo.TicketKey == 0)
                    {
                        continue;
                    }
                    var setting = _siteConfigDic[siteInfo.SiteId];
                    if (setting == null) continue;

                    var dataTpTimer = _thirdPartyBll.GetThirdPartyTimerReferenceByReference(_tpId, _tpType, 0, siteInfo.SiteId.ToString());

                    int last = _iRangMins - 1;
                    DateTime timerTime = (dataTpTimer == null) ? DateTime.Now.AddMinutes(-1 * last) : timerTime = dataTpTimer.StartTime;

                    if (timerTime < DateTime.Now)
                    {
                        var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                        var result = await _wagerPegasusBll.GetWagersAsync(time.startTime, time.endTime, siteInfo.SiteId, setting);
                        if (result)
                        {
                            _thirdPartyBll.SetThirdPartyTimerByReference(_tpId, _tpType, time.endUnix, 0, siteInfo.SiteId.ToString());
                        }
                    }

                    var ts = DateTime.Now - timerTime;
                    var diffMin = (int)ts.TotalMinutes;
                    if (diffMin > 5)
                    {
                        quick = 1;
                    }
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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(_intervalMins);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
