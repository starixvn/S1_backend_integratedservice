using System;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.PP;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.PP;

public class FinancePP: BackgroundService
{
private readonly ILogger<FinancePP> _logger;
    private readonly FinancePPBLL _financePPBLL;
    private Timer _timer;
    private readonly string _tpName = Tp.PP.ToString();
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly FincanceService _financeService;
    private readonly int _tpId = (int)Tp.PP;
    private readonly int _tpType = (int)WagerTimerType.Finance;
    private readonly int _beyond = 2;
    private readonly IConfiguration _configuration;

    public FinancePP(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinancePP>>();
        _financePPBLL = provider.GetRequiredService<FinancePPBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _financeService = provider.GetRequiredService<FincanceService>();
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
            TimeCast reportTime = _financePPBLL.GetReportTime(execTime.StartTime, day);

            DateTime reportDate =
                DateTime.UnixEpoch
                .AddSeconds(reportTime.endUnix);

            var reportData = await _financePPBLL.GetReport(_tpId, null, reportTime);

            // compare
            var dailyData = _financePPBLL.DealReport(_tpId, reportDate, reportData);

            // write to current report
            _financeService.SetFinanceTpDailySummary(dailyData);


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
