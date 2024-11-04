using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerBNG : BackgroundService
    {
        private readonly int _tpId = (int)Tp.BNG;
        private readonly string _tpName = Tp.BNG.ToString();
        private Timer _timer;
        private readonly WagerBNGBLL _wagerBNGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<WagerBNG> _logger;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        public WagerBNG(IServiceProvider provider)
        {
            _wagerBNGBLL = provider.GetRequiredService<WagerBNGBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerBNG>>();
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
                var time = _dateTimeHelper.GetTimeByMinuteRevert(iRangMins, timeZone, _timeFormat, DateTime.UtcNow);

                await _wagerBNGBLL.GetWagerAsync(time.startTime, time.endTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
