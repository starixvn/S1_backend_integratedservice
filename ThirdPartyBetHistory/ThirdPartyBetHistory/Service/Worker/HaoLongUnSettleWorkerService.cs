using Serilog.Context;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class HaoLongUnSettleWorkerService : BackgroundService
    {
        private readonly ILogger<HaoLongUnSettleWorkerService> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WorkerType workerType = WorkerType.MainWager;
        private readonly Tp _tp = Tp.HaoLong;
        private Timer _timer;
        private readonly string _tpName = "HaoLong";
        
        public HaoLongUnSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<HaoLongUnSettleWorkerService>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask,null,TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid());
            LogContext.PushProperty("TpName", _tpName);
            LogContext.PushProperty("WorkerType", workerType);
            var taskProperty = $"{_tpName}:{workerType}";
            try
            {
                var ticketKey = _thirdPartyBLL.GetAllSiteTicketKey(workerType, (int)_tp);
                _logger.LogInformation($"Start: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");
                
                var unSettleList = _thirdPartyBLL.GetHaoLongUnSettleTicket(ticketKey);    
                
                if (!unSettleList.Any())
                {
                    ticketKey = _thirdPartyBLL.GetHaoLongTicketKey(ticketKey);
                }
                
                _thirdPartyBLL.SetAllSiteTicketKey(workerType, (int)_tp, ticketKey);
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
}