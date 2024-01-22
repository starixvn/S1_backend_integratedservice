using Serilog.Context;

using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class RngWorkerService : BackgroundService
    {
        private readonly ILogger<RngWorkerService> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<int, Timer> timerWorkerList;
        private readonly WorkerType workerType = WorkerType.RngWager;
        public RngWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<RngWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            timerWorkerList = new Dictionary<int, Timer>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var siteTpBetHelper in _sabaSiteList)
            {
                try
                {
                    var timer = new Timer(DoTask, siteTpBetHelper, TimeSpan.Zero, TimeSpan.Zero);
                    timerWorkerList.Add(siteTpBetHelper.SiteId, timer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error: RngService");
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

            var lastTicketKey = _thirdPartyBLL.GetSiteTicketKey(workerType, tpBetHelp);
            LogContext.PushProperty("LastTicketKey", lastTicketKey);

            var taskProperty = $"{tpBetHelp.TpName}:{workerType} | SiteId:{tpBetHelp.SiteId} | siteTicketKey:{lastTicketKey}";
            try
            {
                var nextTicketKey = tpBetHelp.AddTpBetRecordDetail(workerType, lastTicketKey).Result;
                LogContext.PushProperty("NextTicketKey", nextTicketKey);

                if (nextTicketKey > lastTicketKey)
                {
                    _thirdPartyBLL.SetSiteTicketKey(workerType, tpBetHelp, nextTicketKey);
                }

                _logger.LogInformation($"Success: {taskProperty} | TicketKey range: {lastTicketKey} ~ {nextTicketKey}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromSeconds(6), TimeSpan.Zero);
            }
        }
    }
}
