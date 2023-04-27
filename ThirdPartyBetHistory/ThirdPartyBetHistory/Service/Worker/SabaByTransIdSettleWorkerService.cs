using Serilog.Context;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaByTransIdSettleWorkerService : BackgroundService
    {
        private readonly ILogger<SabaByTransIdSettleWorkerService> _logger;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<SiteId, Timer> _timerWorkerList;
        private readonly WorkerType workerType = WorkerType.ByTransIdSettleWager;
        private const int SubtractDays = -3;// 檢查幾天前的資料
        private readonly IEnumerable<SettlementType> _settlementTypeList = new List<SettlementType> //檢查類型
        { 
            SettlementType.SettledForValid,
            SettlementType.SettledForInvalid,
            SettlementType.SettledForValidNotRollover,
        }; 

        public SabaByTransIdSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaByTransIdSettleWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _timerWorkerList = new Dictionary<SiteId, Timer>();
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
                    _logger.LogError(ex, "Error: SabaByTransIdSettleWorkerService");
                }
            }

            return Task.CompletedTask;
        }
        
        private void DoTask(object state)
        {
            var tpBetHelp = (IThirdPartyBetHelper)state;
            LogContext.PushProperty("RequestId", Guid.NewGuid());
            LogContext.PushProperty("TpName", tpBetHelp.thirdPartyModel.TpName);
            LogContext.PushProperty("WorkerType", workerType);
            LogContext.PushProperty("SiteId", tpBetHelp.SiteId);

            var taskProperty = $"{tpBetHelp.thirdPartyModel.TpName}:{workerType} | SiteId:{tpBetHelp.SiteId} |";
            
            try
            {
                var startTime = DateTime.UtcNow.Date.AddDays(SubtractDays);
                var endTime = DateTime.UtcNow.AddHours(-1);
                tpBetHelp.UpdateBetRecordByTransId(workerType, startTime, endTime, _settlementTypeList);
                _logger.LogInformation($"Success: {taskProperty} : {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                _timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromHours(8), TimeSpan.Zero);
            }
        }
    }
}
