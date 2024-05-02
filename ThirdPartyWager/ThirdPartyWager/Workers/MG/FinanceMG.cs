using Serilog.Context;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinanceMG : BackgroundService
    {
        private readonly ILogger<FinanceMG> _logger;
        private readonly IMGBLL _mgBll;
        private Timer _timer;
        private readonly string _tpName = Tp.MG.ToString();
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.MG;

        public FinanceMG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceMG>>();
            _mgBll = provider.GetRequiredService<IMGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(GetReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void GetReport(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.Finance;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromHours(4);

            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, timerType);
                var date = lastTime?.StartTime ?? DateTime.UtcNow.BeginningOfTheDay();
                var endTime = date.AddDays(1).AddMilliseconds(-1);
                if (endTime > DateTime.UtcNow)
                {
                    date = date.AddDays(-1);
                }

                var isSuccess = await _mgBll.GetReport(date);
                if (isSuccess)
                {
                    _logger.LogInformation($"MG Get {date:yyyy-MM-dd} report");
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, timerType, new DateTimeOffset(date.AddDays(intervalDays)).ToUnixTimeSeconds());
                }

                if ((int)DateTime.UtcNow.AddDays(-1).Subtract(date).TotalDays > intervalDays)
                {
                    nextTimeSpan = TimeSpan.FromSeconds(30);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MG.GetReport.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {
                _timer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}