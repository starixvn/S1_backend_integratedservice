using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.HL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.HL
{
    public class WagerHL : BackgroundService
    {
        private readonly ILogger<WagerAllBet> _logger;
        private readonly WagerHLBLL _wagerHLBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer timer;
        private readonly int intervalMins = 30;
        private readonly int iRangMins = 61;
        private readonly int tpId = (int)Tp.HaoLong;
        private readonly string _tpName = Tp.HaoLong.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = 8;
        private readonly IHostEnvironment _environment;

        public WagerHL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerAllBet>>();
            _wagerHLBLL = provider.GetRequiredService<WagerHLBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoHLReportTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoHLReportTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            if (CheckHaoLongBusyTime())
            {
                _logger.LogInformation($"TP - HaoLong Busy Time");
                return;
            }
            
            try
            {
                var nowTime = DateTime.Now;
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                
                if (timerTime < nowTime)
                {
                    var time = _dateTimeHelper.GetTimeByDay(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var result = await _wagerHLBLL.GetWagersAsync(time.startTime, time.endTime);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                    
                    if(timerTime.Day < nowTime.Day)
                    {
                        _ = await _wagerHLBLL.SetSettledWager(time.startTime, time.endTime);
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
                    var dueTimeSpan = TimeSpan.FromSeconds(30);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.FromHours(1));
                }

            }
        }

        private bool CheckHaoLongBusyTime()
        {
            var utcNow = DateTime.UtcNow;
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var timeInGmtPlus8 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus8);
            return timeInGmtPlus8.TimeOfDay >= new TimeSpan(15, 0, 0) && timeInGmtPlus8.TimeOfDay <= new TimeSpan(20, 0, 0);
        }
    }
}