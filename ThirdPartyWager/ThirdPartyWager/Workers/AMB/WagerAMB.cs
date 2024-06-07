using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerAMB : BackgroundService
{
    private readonly string _tpName = Tp.AMB.ToString();
    private Timer _timer;
    private readonly WagerAMBBLL _wagerAMBBLL;
    private readonly ILogger<WagerGA> _logger;
    private readonly int timeRange = 5;
    private readonly int tpType = (int)WagerTimerType.Wager;
    private readonly int runTime = 5;
    public WagerAMB(IServiceProvider provider)
    {
        _wagerAMBBLL = provider.GetRequiredService<WagerAMBBLL>();
        _logger = provider.GetRequiredService<ILogger<WagerGA>>();
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
        var quick = false;

        try
        {
            _logger.LogInformation($"DoAMBReport Start DoAMBReportTask Job Time : : +0 {DateTime.Now:yyyy-MM-ddTHH:mm:ss}");
            quick = await _wagerAMBBLL.ExecWagerWorker(tpType, timeRange);
           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            if (quick)
            {
                var dueTimeSpan = TimeSpan.FromSeconds(30);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
            else
            {
                var dueTimeSpan = TimeSpan.FromMinutes(runTime);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }

}