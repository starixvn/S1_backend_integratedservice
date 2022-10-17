using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.KM;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerKM : BackgroundService
    {
        private readonly ILogger<WagerKM> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly KMBLL _kmBLL;
        private Timer _settleTimer;

        private const int TpId = (int)Tp.KM;
        private readonly string _tpName = Tp.KM.ToString();
        private const int TpType = 52; // (int)WagerTimerType.Wager
        private const int TimeZone = 8;

        public WagerKM(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerKM>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _kmBLL = provider.GetRequiredService<KMBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _settleTimer = new Timer(DoSettle, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 呼叫KM API 取得歷史投注紀錄
        /// 1. 初始查詢區間為過去20分鐘到現在之間的資料
        /// 2. 排程每3分鐘執行一次
        /// 3. 下次查詢區間為"上次最後查詢時間"到現在
        /// </summary>
        /// <param name="state"></param>
        private async void DoSettle(object? state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var doQuickly = false;
            var timerType = 1; //Settle
            var dataRangeMins = 20;
            var intervalMins = 3;
            var intervalSec = 30;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, TpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * dataRangeMins);

                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByMinute(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm:ss", thisTime);
                    //var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(TimeZone, 0, 0)).ToUnixTimeSeconds();
                    //if (timeCast.endUnix < cvTime)
                    //{
                    var isSuccess = await _kmBLL.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"KM Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, TpType, timeCast.endUnix);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerKM.DoSettle.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                _settleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}