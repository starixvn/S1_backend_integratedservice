using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoDragonGaming : BackgroundService
    {
        private readonly int _tpId = (int)Tp.DragonGaming;
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private readonly string _tpName = Tp.DragonGaming.ToString();
        private Timer _timer;
        private readonly Dictionary<string, string> _setting;
        private readonly WagerBNGBLL _wagerBNGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerRedoDragonGaming> _logger;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int intervalMins = 5;

        public WagerRedoDragonGaming(IServiceProvider provider)
        {
            _setting = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(Tp.DragonGaming.ToString());
            _wagerBNGBLL = provider.GetRequiredService<WagerBNGBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
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
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                var nowDateTime = DateTime.Now;

                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = _tpId,
                        Type = _tpType,
                        StartTime = nowDateTime.AddHours(-2).AddMinutes(-15),
                    };
                }
                else
                {
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (dataTpTimer.StartTime <= nowDateTime.AddHours(-2).AddMinutes(-15))
                {
                    var time = _dateTimeHelper.GetTimeByMinute(iRangMins - 1, timeZone, _timeFormat, dataTpTimer.StartTime);
                    await _wagerBNGBLL.GetWagerAsync(_setting, time.startTime, time.endTime);

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
