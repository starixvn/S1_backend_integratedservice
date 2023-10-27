using Serilog.Context;

using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaVgamingUnSettleWorkerService : BackgroundService
    {
        private readonly ILogger<SabaVgamingUnSettleWorkerService> _logger;
        private readonly Dictionary<int, IThirdPartyBetHelper> _sabaSiteList;
        private readonly WorkerType workerType = WorkerType.VgamingUnSettle;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer _timer;
        private readonly Tp _tp;

        public SabaVgamingUnSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaVgamingUnSettleWorkerService>>();
            var betHelperList = provider.GetServices<IThirdPartyBetHelper>();
            _tp = betHelperList.First().Tp;
            _sabaSiteList = betHelperList.ToDictionary(i => i.SiteId, i => i);
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);                  
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            var tpName = _tp.ToString();
            LogContext.PushProperty("RequestId", Guid.NewGuid());
            LogContext.PushProperty("TpName", tpName);
            LogContext.PushProperty("WorkerType", workerType);
            var taskProperty = $"{tpName}:{workerType}";
            try
            {
                var ticketKey = _thirdPartyBLL.GetAllSiteTicketKey(workerType, (int)_tp);
                _logger.LogInformation($"Start: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");

                var unSettleList = _thirdPartyBLL.GetVgamingUnSettleTicket(ticketKey);                
                foreach (var unsettle in unSettleList)
                {
                    _sabaSiteList[unsettle.SiteId].ResubmitProcess(unsettle);
                    ticketKey = unsettle.TicketKey;
                }

                // 沒有未結算的Vgaming注單，從DB取得下一次要檢查的TicketKey
                if (!unSettleList.Any())
                {
                    ticketKey = _thirdPartyBLL.GetVgamingTicketKey(ticketKey);
                }

                _thirdPartyBLL.SetAllSiteTicketKey(workerType, (int)_tp, ticketKey);
                _logger.LogInformation($"Success: {taskProperty} | SQ is :{ticketKey} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Saba Vgaming UnSettle Exception: {taskProperty}");
            }
            finally
            {
                _timer.Change(TimeSpan.FromMinutes(5), TimeSpan.Zero);
            }
        }
    }
}
