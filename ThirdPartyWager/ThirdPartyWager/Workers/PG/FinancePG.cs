using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.PG;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.PG;

public class FinancePG : BackgroundService
{
    private readonly ILogger<FinancePG> _logger;
    private readonly FinancePGBLL _financePGBLL;
    private Timer _timer;
    private readonly string _tpName = Tp.PG.ToString();
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly FinanceService _financeService;
    private readonly int _tpId = (int)Tp.PG;
    private readonly int _tpType = (int)WagerTimerType.Finance;
    private readonly int _beyond = 2;
    private readonly IConfiguration _configuration;

    public FinancePG(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinancePG>>();
        _financePGBLL = provider.GetRequiredService<FinancePGBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _financeService = provider.GetRequiredService<FinanceService>();
        _configuration = provider.GetRequiredService<IConfiguration>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private async void DoReport(object state)
    {
        var quick = 0;

        try
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            var res = await ExecReportAsync();

            if (res)
            {
                quick = 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            var interval = quick == 1
                ? _configuration.GetValue<int>("FinanceJobInterval:WhenError")!
                : _configuration.GetValue<int>("FinanceJobInterval:WhenSuccess")!;
            var dueTimeSpan = TimeSpan.FromMinutes(interval);
            _logger.LogInformation("{TpName}: update interval to {Interval} ", _tpName, interval);
            _timer.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }

    private async Task<bool> ExecReportAsync()
    {
        var quick = false;

        try
        {
            var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

            var execTime = new ThirdPartyTimerModel()
            {
                TpId = _tpId,
                Type = _tpType,
                StartTime = (dataTpTimer == null) ? DateTime.UtcNow.AddDays(-3) : dataTpTimer.StartTime.AddSeconds(1),
            };

            var day = 0;
            var reportTime = _financePGBLL.GetReportTime(execTime.StartTime, day);

            var reportDate =
                DateTime.UnixEpoch
                .AddSeconds(reportTime.endUnix);

            var reportData = await _financePGBLL.GetReport(_tpId, null, reportTime);

            

            if (reportData.Count > 0)
            {
                // compare
                var dailyData = _financePGBLL.DealReport(_tpId, reportDate, reportData);

                // write to current report
                foreach (var data in dailyData)
                {
                    // write
                    _financeService.SetFinanceTpDailySummary(data);
                }
            }

            // update timer

            _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, reportTime.endUnix);

            var diffDay = (int)(DateTime.Now.Date - reportDate.Date).TotalDays;

            if (diffDay > _beyond)
            {
                quick = true;
            }

            _logger.LogInformation($"{_tpName} Get {reportDate:yyyy-MM-dd} report");
            return quick;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            quick = true;
        }

        _logger.LogInformation($"Report Worker Execute End {_tpId}");
        return quick;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}