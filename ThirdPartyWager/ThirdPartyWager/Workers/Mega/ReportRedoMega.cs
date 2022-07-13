using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class ReportRedoMega : BackgroundService
    {
        private readonly ILogger<ReportRedoMega> _logger;
        private readonly WagerMegaBLL _wagerMegaBLL;
        private readonly CommonBLL _commonBLL;
        private readonly IHostEnvironment _environment;

        private Timer timer;

        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private readonly int timeRange = 1440; //in minutes
        private readonly int quickTime = 1200; // in seconds
        private readonly int runTime = 1; // in day
        private readonly string _tpName = Tp.Mega.ToString();

        public ReportRedoMega(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ReportRedoMega>>();
            _wagerMegaBLL = provider.GetRequiredService<WagerMegaBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = 0;
            var failed = 0;
            var tasks = new List<Task>();
            try
            {
                var tpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.Mega);

                foreach (var item in tpCurrencyList)
                {
                    if (!_environment.IsProduction() && item.TpCurrencyId == "USD")
                    {
                        continue;
                    }

                    var currency = item.TpCurrencyId;
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var res = _wagerMegaBLL.ExecReportPlayerListWorker(tpType, currency, timeRange);
                            if (res.Result)
                            {
                                quick = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            Interlocked.Increment(ref failed);
                        }
                    }));

                }
                var t = Task.WhenAll(tasks);
                t.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = quick == 1 ? TimeSpan.FromSeconds(quickTime) : TimeSpan.FromDays(runTime);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
