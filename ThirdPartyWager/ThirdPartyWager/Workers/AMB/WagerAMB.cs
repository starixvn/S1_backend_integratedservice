using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerAMB : BackgroundService
{
    private Timer _timer;
    private readonly string _tpName = Tp.AMB.ToString();
    private readonly WagerAMBBLL _wagerAMBBLL;
    private readonly ILogger<WagerAMB> _logger;
    private readonly int _timeRange = 5;
    private readonly int _tpType = (int)WagerTimerType.Wager;
    private readonly int _runTime = 1;

    public WagerAMB(IServiceProvider provider)
    {
        _wagerAMBBLL = provider.GetRequiredService<WagerAMBBLL>();
        _logger = provider.GetRequiredService<ILogger<WagerAMB>>();
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
            quick = await _wagerAMBBLL.ExecWagerWorker(_tpType, _timeRange);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            if (quick)
            {
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.Zero);
            }
            else
            {
                _timer.Change(TimeSpan.FromMinutes(_runTime), TimeSpan.Zero);
            }
        }
    }

}