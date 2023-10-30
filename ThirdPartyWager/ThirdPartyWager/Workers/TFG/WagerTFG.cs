using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerTFG : BackgroundService
    {
        private readonly ILogger<WagerTFG> _logger;
        private Timer timer;
        private readonly int tpId = (int)Tp.TFG;
        private readonly string _tpName = Tp.TFG.ToString();
        private readonly CommonBLL _commonBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int dataRangeMins = 15;
        private readonly int intervalMins = 1;
        private readonly int intervalSec = 5;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly DateTimeHelper _dateTimeHelper;
        private int quick = 0;
        private readonly int timeZone = 0;
        private readonly WagerTFGBLL _wagerTFGBLL;

        public WagerTFG(
            ILogger<WagerTFG> logger,
            CommonBLL commonBLL,
            ThirdPartyBLL thirdPartyBLL,
            DateTimeHelper dateTimeHelper,
            WagerTFGBLL wagerTFGBLL
        )
        {
            _logger = logger;
            _commonBLL = commonBLL;
            _thirdPartyBLL = thirdPartyBLL;
            _dateTimeHelper = dateTimeHelper;
            _wagerTFGBLL = wagerTFGBLL;
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
                var TpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.TFG);
                var setting = _thirdPartyBLL.GetThirdPartySetting((int)Tp.TFG);
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                var apiurl = setting["ApiUrl"];
                var SetTimer = true;
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = dataRangeMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(-1);
                    last = dataRangeMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ss.fffZ", timerTime);
                    foreach (var TpCurrency in TpCurrencyList)
                    {
                        if (setting.ContainsKey($"PrivateToken_{TpCurrency.TpCurrencyId}"))
                        {
                            var result = await _wagerTFGBLL.GetWagersAsync(apiurl, setting[$"PrivateToken_{TpCurrency.TpCurrencyId}"], time.startTime, time.endTime);
                            if (!result)
                            {
                                SetTimer = false;
                            }
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
                    var dueTimeSpan = TimeSpan.FromSeconds(intervalSec);
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
