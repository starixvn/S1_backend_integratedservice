using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerEdayUpdate : BackgroundService
    {
        private readonly ILogger<WagerEdayUpdate> _logger;
        private readonly WagerEdayBLL _wagerEdayBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _tpId = (int)Tp.Eday;
        private Timer timer;
        private readonly int intervalMins = 1;
        private readonly int iRangMins = 60;
        private readonly string _tpName = Tp.Eday.ToString();
        private readonly int tpType = (int)WagerTimerType.UnSettle;
        private readonly int timeZone = 0;

        public WagerEdayUpdate(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerEdayUpdate>>();
            _wagerEdayBLL = provider.GetRequiredService<WagerEdayBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoEdayUpdateTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoEdayUpdateTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            try
            {
                var nowTime = DateTime.Now;
                _logger.LogInformation($"DoEday Start DoEdayUpdateTask Job Time : : +0 {nowTime:yyyy-MM-ddTHH:mm:ss}");

                var result = false;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, tpType);

                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;

                if (timerTime < nowTime)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ss", timerTime);
                    
                    result = await _wagerEdayBLL.GetEdayBillDetailAsync();
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
                var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}