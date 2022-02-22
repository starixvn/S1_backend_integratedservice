using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class ReportMega : BackgroundService
    {
        private readonly ILogger<ReportMega> _logger;
        private readonly WagerMegaBLL _wagerMegaBLL;

        private Timer timerTHB;
        private Timer timerMYR;
        private Timer timerIDR;
        private readonly int tpType = 1;
        private readonly int timeRange = 1440; //in minutes (24 * 60)
        private readonly int quickTime = 300; // in seconds
        private readonly int runTime = 600; // in seconds
        private readonly string _tpName = Tp.Mega.ToString();

        public ReportMega(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ReportMega>>();
            _wagerMegaBLL = provider.GetRequiredService<WagerMegaBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerTHB = new Timer(DoTaskTHB, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            timerMYR = new Timer(DoTaskMYR, null, TimeSpan.FromSeconds(2), TimeSpan.Zero);
            timerIDR = new Timer(DoTaskIDR, null, TimeSpan.FromSeconds(3), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskTHB(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            bool quick = false;
            var currency = "THB";
            try
            {
                quick = await _wagerMegaBLL.ExecReportPlayerListWorker(tpType, currency, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                timerTHB.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoTaskMYR(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            bool quick = false;
            var currency = "MYR";
            try
            {
                quick = await _wagerMegaBLL.ExecReportPlayerListWorker(tpType, currency, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                timerMYR.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoTaskIDR(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            bool quick = false;
            var currency = "IDR";
            try
            {
                quick = await _wagerMegaBLL.ExecReportPlayerListWorker(tpType, currency, timeRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromSeconds(runTime);
                timerIDR.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerTHB?.Change(Timeout.Infinite, 0);
            timerMYR?.Change(Timeout.Infinite, 0);
            timerIDR?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
