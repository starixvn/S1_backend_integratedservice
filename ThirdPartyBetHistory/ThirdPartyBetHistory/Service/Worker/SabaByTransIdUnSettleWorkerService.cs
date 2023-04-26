using Serilog.Context;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaByTransIdUnSettleWorkerService : BackgroundService
    {
        private readonly ILogger<SabaByTransIdUnSettleWorkerService> _logger;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<int, Timer> _timerWorkerList;
        private const WorkerType WorkerType = Model.enums.WorkerType.ByTransIdUnSettleWager;

        public SabaByTransIdUnSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaByTransIdUnSettleWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _timerWorkerList = new Dictionary<int, Timer>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var timer = new Timer(DoTask, _sabaSiteList, TimeSpan.Zero, TimeSpan.Zero);
                _timerWorkerList.Add(1, timer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: SabaByTransIdUnSettleWorkerService");
            }

            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            var taskProperty = "";

            try
            {
                var tpBetHelps = (IEnumerable<IThirdPartyBetHelper>)state;
                var today = Convert.ToDateTime(DateTime.UtcNow.ToString("yyyy-MM-dd"));
                foreach (var tpBetHelp in tpBetHelps)
                {
                    LogContext.PushProperty("RequestId", Guid.NewGuid());
                    LogContext.PushProperty("TpName", tpBetHelp.thirdPartyModel.TpName);
                    LogContext.PushProperty("WorkerType", WorkerType);
                    LogContext.PushProperty("SiteId", tpBetHelp.SiteId);
                    taskProperty = $"{tpBetHelp.thirdPartyModel.TpName}:{WorkerType} | SiteId:{tpBetHelp.SiteId}";
                    
                    for (var day = -7; day < -1; day++)
                    {
                        await tpBetHelp.UpdateBetRecordByTransId(WorkerType, today, 0, day);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                _timerWorkerList[1].Change(TimeSpan.FromHours(8), TimeSpan.Zero);
            }
        }
    }
}
