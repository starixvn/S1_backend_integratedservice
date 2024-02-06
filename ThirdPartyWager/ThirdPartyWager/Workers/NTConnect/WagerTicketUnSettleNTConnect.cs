using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers;

public class WagerTicketUnSettleNTConnect : BackgroundService
{
    private readonly ILogger<WagerTicketUnSettleNTConnect> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly WagerNTConnectService _wagerNTConnectService;
    private readonly WorkerType workerType = WorkerType.MainWager;
    private readonly Tp _tp = Tp.HaoLong;
    private Timer _timer;
    private readonly string _tpName = "NTConnect";
    
    public WagerTicketUnSettleNTConnect(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerTicketUnSettleNTConnect>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _wagerNTConnectService = provider.GetRequiredService<WagerNTConnectService>();
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoHLTicketTask,null,TimeSpan.Zero, TimeSpan.Zero);
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
                
            var unSettleList = _wagerNTConnectService.GetNTCConnectBetUnSettledList(ticketKey);    
                
            if (!unSettleList.Any())
            {
                ticketKey = _wagerNTConnectService.GetNTCConnectTicketKey(ticketKey);
            }
            else
            {
                _thirdPartyBLL.SetAllSiteTicketKey(workerType, (int)_tp, unSettleList.FirstOrDefault().TicketKey);
            }
            _logger.LogInformation($"Success: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
        }
        finally
        {
            _timer.Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
        }
    }
}