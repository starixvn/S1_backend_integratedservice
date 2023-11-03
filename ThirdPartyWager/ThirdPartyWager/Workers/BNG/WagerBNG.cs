using Serilog.Context;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.BNG;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.BNG
{
    public class WagerBNG : BackgroundService
    {
        private readonly int _tpId = (int)Tp.BNG;
        private readonly string _tpName = Tp.BNG.ToString();
        private Timer _timer;
        private readonly Dictionary<string, string> _setting;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerBNGBLL _wagerBNGBLL;
        private readonly CommonBLL _commonBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<WagerBNG> _logger;
        private readonly int timeZone = -4;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        public WagerBNG(IServiceProvider provider)
        {
            _setting = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(Tp.BNG.ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerBNGBLL = provider.GetRequiredService<WagerBNGBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
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

                await _wagerBNGBLL.GetWagerAsync(_setting, time.startTime, time.endTime);
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
