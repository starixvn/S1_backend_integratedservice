using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNTConnect : BackgroundService
    {
        private readonly ILogger<WagerNTConnect> _logger;
        private readonly WagerNTConnectBLL _wagerNTConnectBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _tpId = (int)Tp.NTConnect;
        private Timer timer;
        private readonly int intervalMins = 3;
        private readonly int iRangMins = 15;
        private readonly string _tpName = Tp.NTConnect.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = -4;

        public WagerNTConnect(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerNTConnect>>();
            _wagerNTConnectBLL = provider.GetRequiredService<WagerNTConnectBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoNTConnectReportTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoNTConnectReportTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            
            try
            {
                var nowTime = DateTime.Now;
                _logger.LogInformation($"DoNTConnect Start DoNTConnectReportTask Job Time : : +0 {nowTime:yyyy-MM-ddTHH:mm:ss}");

                var result = false;
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                
                if (timerTime < nowTime)
                {
                     var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm", timerTime);
                     
                     result =  await _wagerNTConnectBLL.GetWagersAsync(time.startTime, time.endTime);

                     if (result)
                     {
                         _thirdPartyBLL.SetThirdPartyTimer(_tpId, tpType, time.endUnix);
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
    }
}