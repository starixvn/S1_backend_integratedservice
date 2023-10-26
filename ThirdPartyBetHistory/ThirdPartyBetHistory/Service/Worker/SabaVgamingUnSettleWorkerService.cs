using Serilog.Context;

using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaVgamingUnSettleWorkerService : BackgroundService
    {
        private readonly ILogger<SabaVgamingUnSettleWorkerService> _logger;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<int, Timer> _timerWorkerList;
        private readonly WorkerType workerType = WorkerType.VgamingUnSettle;
        private readonly ThirdPartyBLL _thirdPartyBLL;


        public SabaVgamingUnSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaVgamingUnSettleWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _timerWorkerList = new Dictionary<int, Timer>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var siteTpBetHelper in _sabaSiteList)
            {
                try
                {
                    var timer = new Timer(DoTask, siteTpBetHelper, TimeSpan.Zero, TimeSpan.Zero);
                    _timerWorkerList.Add(siteTpBetHelper.SiteId, timer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error: SabaVgamingUnSettleWorkerService");
                }
            }
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            var tpBetHelp = (IThirdPartyBetHelper)state;
            LogContext.PushProperty("RequestId", Guid.NewGuid());
            LogContext.PushProperty("TpName", tpBetHelp.TpName);
            LogContext.PushProperty("WorkerType", workerType);
            LogContext.PushProperty("SiteId", tpBetHelp.SiteId);

            var ticketKey = _thirdPartyBLL.GetSiteTicketKey(workerType, tpBetHelp);
            var taskProperty = $"{tpBetHelp.TpName}:{workerType} | SiteId:{tpBetHelp.SiteId} | siteTicketKey:{ticketKey}";
            try
            {
                var unSettleList = _thirdPartyBLL.GetVgamingUnSettleTicket(tpBetHelp.SiteId, ticketKey);
                foreach (var unsettle in unSettleList)
                {
                    tpBetHelp.ResubmitProcess(unsettle);
                    ticketKey = unsettle.TicketKey;
                }

                // 沒有未結算的Vgaming注單，從DB取得下一次要檢查的TicketKey
                if (!unSettleList.Any())
                {
                    ticketKey = _thirdPartyBLL.GetVgamingTicketKey(tpBetHelp.SiteId, ticketKey);
                }

                _thirdPartyBLL.SetSiteTicketKey(workerType, tpBetHelp, ticketKey);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                _timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromMinutes(10), TimeSpan.Zero);
            }
        }
    }
}
