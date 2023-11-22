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
        private readonly int intervalMins = 15;
        private readonly int iRangMins = 10;
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
            
            try
            {
                var nowTime = DateTime.Now;
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                
                if (CheckHaoLongTime(1))
                {
                    _logger.LogInformation($"TP - HaoLong Busy Time");
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    return;
                }
                
                if (timerTime < nowTime)
                {
                     var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                     var result = await _wagerHLBLL.GetWagersAsync(time.startTime, time.endTime);
                     if (result)
                     {
                         _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                     }
                    
                     if(CheckHaoLongTime(2))
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
                    var dueTimeSpan = TimeSpan.FromMinutes(1);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }

            }
        }

        private bool CheckHaoLongTime(int timeType)
        {
            //1:拉單
            //2:結算時間
            
            var utcNow = DateTime.UtcNow;
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var timeInGmtPlus8 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus8);
            return timeType switch
            {
                1 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(14, 55, 0) &&
                     timeInGmtPlus8.TimeOfDay <= new TimeSpan(20, 5, 0),
                2 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(20, 30, 0) &&
                     timeInGmtPlus8.TimeOfDay <= new TimeSpan(21, 30, 0),
                _ => false
            };}
    }
}