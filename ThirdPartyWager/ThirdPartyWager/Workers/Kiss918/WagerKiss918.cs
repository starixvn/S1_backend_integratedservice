using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerKiss918 : BackgroundService
{
    private readonly ILogger<WagerKiss918> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly WagerKiss918BLL _wagerKiss918BLL;
    private readonly CommonBLL _commonBLL;

    private readonly int tpId = (int)Tp.Kiss918;
    private readonly int tpType = (int)WagerTimerType.Wager;
    private readonly int intervalMins = 3;
    private readonly int intervalFasterMins = 1;
    private readonly int iRangMins = 60;

    private const int TimeZone = 0;
    private Timer _timer;
    private bool isFaster = false;


    public WagerKiss918(WagerKiss918BLL wagerKiss918Bll,
                            ThirdPartyBLL thirdPartyBll,
                            ILogger<WagerKiss918> logger,
                            DateTimeHelper dateTimeHelper,
                            CommonBLL commonBll)
    {
        _wagerKiss918BLL = wagerKiss918Bll;
        _thirdPartyBLL = thirdPartyBll;
        _logger = logger;
        _dateTimeHelper = dateTimeHelper;
        _commonBLL = commonBll;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private async void DoTask(object state)
    {
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
        LogContext.PushProperty("Tp", Tp.Kiss918.ToString());
        try
        {
            var TpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.Kiss918);
            foreach (var item in TpCurrencyList)
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, tpType, item.TpCurrencyId);
                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = iRangMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime;
                    last = iRangMins - 1;
                }

                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, TimeZone, "yyyy-MM-dd HH:mm:ss.fff", timerTime);
                    var result = await _wagerKiss918BLL.GetWagersAsync(tpId, time.startTime);

                    if (result)
                        _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, tpType, time.endUnix, item.TpCurrencyId);
                }

                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin > 5)
                    isFaster = true;
                else
                    isFaster = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            if (isFaster)
            {
                var dueTimeSpan = TimeSpan.FromSeconds(intervalFasterMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
            else
            {
                var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
