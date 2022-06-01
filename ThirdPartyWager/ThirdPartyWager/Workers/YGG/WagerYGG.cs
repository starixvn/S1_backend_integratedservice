using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.YGG;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.YGG
{
    public class WagerYGG : BackgroundService
    {
        private readonly ILogger<WagerYGG> _logger;
        private readonly WagerYGGBLL _wagerYGGBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly string _tpName = Tp.YGG.ToString();

        private Timer _timer;

        private const int TpId = (int)Tp.YGG;
        private const int TimeZone = 0; // 時區
        private const int DataRangeMinute = 15; // Job 每次撈去 15分鐘的內的注單
        private const int QuickTime = 3; // in seconds，如果來不急，只停3秒，才能快點追上 執行時間
        private const int IntervalSec = 60; // in seconds 60秒跑一次
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerYGG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerYGG>>();
            _wagerYGGBLL = provider.GetRequiredService<WagerYGGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
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
            var doQuickly = false;

            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, (int)GameType.YGG_RNG);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * DataRangeMinute);

                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByMinute(DataRangeMinute, TimeZone, DateTimeFormat, thisTime);
                    var isSuccess = await _wagerYGGBLL.GetWagersAsync(timeCast.startTime, timeCast.endTime); // 拉取注單
                    if (isSuccess)
                    {
                        var setEndTime = new DateTimeOffset(thisTime)
                            .AddMinutes(1).ToOffset(new TimeSpan(TimeZone, 0, 0)).ToUnixTimeSeconds();
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, (int)GameType.YGG_RNG, setEndTime); // 如果成功要 Set 下一次執行時間
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > DataRangeMinute)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerYGG.DoTask.Exception", ex.Message);
            }
            finally
            {
                var dueTimeSpan = doQuickly ? TimeSpan.FromSeconds(QuickTime) : TimeSpan.FromSeconds(IntervalSec);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
