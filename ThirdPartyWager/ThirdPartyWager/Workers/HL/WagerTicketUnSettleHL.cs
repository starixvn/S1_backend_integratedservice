using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerTicketUnSettleHL : BackgroundService
{
    private Timer _timer;
    private readonly ILogger<WagerTicketUnSettleHL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly WagerHLService _wagerHLService;
    private readonly WorkerType workerType = WorkerType.MainWager;
    private readonly Tp _tp = Tp.HaoLong;
    private readonly string _tpName = "HaoLong";

    public WagerTicketUnSettleHL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerTicketUnSettleHL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _wagerHLService = provider.GetRequiredService<WagerHLService>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoHLTicketTask, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private void DoHLTicketTask(object state)
    {
        LogContext.PushProperty("RequestId", Guid.NewGuid());
        LogContext.PushProperty("TpName", _tpName);
        LogContext.PushProperty("WorkerType", workerType);
        var taskProperty = $"{_tpName}:{workerType}";
        try
        {
            var ticketKey = _thirdPartyBLL.GetAllSiteTicketKey(workerType, (int)_tp);
            _logger.LogInformation($"Start: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");

            var wagerList = _wagerHLService.GetHLBetUnSettledList(ticketKey);

            var getUnSettledList = wagerList.Where(wager => wager.SettleFlag == 0).ToList();

            if (getUnSettledList.Any())
            {
                _thirdPartyBLL.SetAllSiteTicketKey(workerType, (int)_tp, getUnSettledList.FirstOrDefault().TicketKey);
            }
            else
            {
                ticketKey = _wagerHLService.GetHaoLongTicketKey(ticketKey - 1);
                _thirdPartyBLL.SetAllSiteTicketKey(workerType, (int)_tp, ticketKey);

            }
            _logger.LogInformation($"Success: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
        }
        finally
        {
            _timer.Change(TimeSpan.FromMinutes(90), TimeSpan.Zero);
        }
    }
}