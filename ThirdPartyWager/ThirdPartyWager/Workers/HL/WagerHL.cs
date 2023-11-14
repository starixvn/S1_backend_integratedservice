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
        private readonly WagerHLBLL _wageHLBLL;
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
            _wageHLBLL = provider.GetRequiredService<WagerHLBLL>();
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
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
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
                    last = iRangMins;
                }
                
                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByDay(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    //if (time.endUnix < cvTime)
                    //{
                    var result = await _wageHLBLL.GetWagersAsync(time.startTime, time.endTime);
                    if (result)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
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
    }
}