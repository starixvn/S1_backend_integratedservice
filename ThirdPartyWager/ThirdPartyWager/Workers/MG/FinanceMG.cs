using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.MG;
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
        private readonly int tpId = (int)Tp.MG;

        public FinanceMG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceMG>>();
            _mgBll = provider.GetRequiredService<IMGBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(GetReport,null,  TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void GetReport(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var doQuickly = false;
            var timerType = 1;
            var intervalDays = 1;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(tpId, timerType);
                var date = lastTime?.StartTime ?? DateTime.UtcNow.Date.AddDays(-14);

                if (date < DateTime.UtcNow)
                {
                    var isSuccess = await _mgBll.GetReport(date);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"MG Get {date:yyyy-MM-dd} report");
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, timerType, new DateTimeOffset(date.AddDays(intervalDays)).ToUnixTimeSeconds());
                    }
                }

                if ((int)DateTime.UtcNow.Subtract(date).TotalDays > intervalDays)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MG.GetReport.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(intervalDays);
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