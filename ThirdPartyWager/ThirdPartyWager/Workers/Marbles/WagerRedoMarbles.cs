using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoMarbles : BackgroundService
    {
        private readonly int _tpId = (int)Tp.Marbles;
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private readonly string _tpName = Tp.Marbles.ToString();
        private Timer _timer;
        private readonly Dictionary<int, MarblesConfig> _siteConfigDic;
        private readonly WagerMarblesBLL _wagerMarblesBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerRedoMarbles> _logger;
        private readonly int _timeZone = 0;
        private readonly int _iRangMins = 30;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int _runTime = 1;
        private readonly int _quickTime = 5; // in minute
        public WagerRedoMarbles(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoMarbles>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerMarblesBLL = provider.GetRequiredService<WagerMarblesBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MarblesConfig>(Tp.Marbles.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoUnsettleCheck, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoUnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            bool quick = false;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                var nowDateTime = DateTime.Now;
                var result = 0;

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

                quick = CheckQuick(dataTpTimer.StartTime, 100);
                var cvTime = DateTimeOffset.Now.AddHours(-1).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
                var time = _dateTimeHelper.GetTimeByMinute(_iRangMins - 1, _timeZone, _timeFormat, dataTpTimer.StartTime);

                if (time.endUnix <= cvTime)
                {
                    var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                    foreach (var site in siteList)
                    {
                        var _setting = _siteConfigDic[site.SiteId];

                        if (_setting == null) continue;

                        result = await _wagerMarblesBLL.GetWagerAsync(_setting, time.startUnix, time.endUnix);

                        if (result < 0)
                        {
                            //第三方因素或Insert失敗回傳為-1，直接進break不再更新時間。
                            break;
                        }
                    }

                    if (result > 0)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = quick ? TimeSpan.FromMinutes(_quickTime) : TimeSpan.FromHours(_runTime);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }

    }
}