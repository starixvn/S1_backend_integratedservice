using Serilog.Context;
using ThirdPartyWager.Service.BLL.SabaMatch;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers.SabaMatch;

public class SabaMatches : BackgroundService
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SabaMatches> _logger;
    private readonly SabaMatchBLL _sabaMatchBLL;
    private Timer _timerSabaOdds;
    private readonly string _tpName = "SabaOdds";

    public SabaMatches(IServiceProvider provider)
    {
        _environment = provider.GetRequiredService<IHostEnvironment>();
        _logger = provider.GetRequiredService<ILogger<SabaMatches>>();
        _sabaMatchBLL = provider.GetRequiredService<SabaMatchBLL>();
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timerSabaOdds = new Timer(DoSabaMatchAsync, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }
    private async void DoSabaMatchAsync(object state)
    {
        LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
        LogContext.PushProperty("Tp", _tpName);
        LogContext.PushProperty("JobName", nameof(DoSabaMatchAsync));

        var isError = false;
        try
        {
            isError = await _sabaMatchBLL.UpdateHotMatch();
        }
        catch (Exception ex)
        {
            isError = true;
            _logger.LogError(ex, GetCurrentAsyncMethod());
        }
        finally
        {
            var dueTimeSpan = TimeSpan.FromMinutes(10);
            if (_environment.IsDevelopment())
            {
                dueTimeSpan = TimeSpan.FromSeconds(30);
            }

            if (isError)
            {
                dueTimeSpan = TimeSpan.FromSeconds(5);
            }

            _timerSabaOdds.Change(dueTimeSpan, TimeSpan.Zero);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timerSabaOdds?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}
