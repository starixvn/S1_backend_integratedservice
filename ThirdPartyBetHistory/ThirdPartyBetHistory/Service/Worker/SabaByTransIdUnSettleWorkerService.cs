using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaByTransIdUnSettleWorkerService : BackgroundService
    {
        private readonly ILogger<SabaByTransIdUnSettleWorkerService> _logger;
        private readonly Dictionary<int, IThirdPartyBetHelper> _sabaSiteList;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ICacheHelper _redisHelper;
        private Timer _timer;
        private readonly WorkerType workerType = WorkerType.ByTransIdUnSettleWager;
        private const int SubtractDays = -7;// 檢查幾天前的資料
        private readonly Tp _tp;


        public SabaByTransIdUnSettleWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaByTransIdUnSettleWorkerService>>();
            var betHelperList = provider.GetServices<IThirdPartyBetHelper>();
            _tp = betHelperList.First().Tp;
            _sabaSiteList = betHelperList.ToDictionary(i => i.SiteId, i => i);
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
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
            var taskProperty = $"{tpName} | {workerType}";
            var redisKey = $"{tpName}:{workerType}";

            try
            {
                var startTime = DateTime.UtcNow.Date.AddDays(SubtractDays);
                var endTime = DateTime.UtcNow.AddMinutes(-30);
                taskProperty += $" | {startTime:yyyy-MM-dd HH:mm} ~ {endTime:yyyy-MM-dd HH:mm}";

                //先從Redis拿取超過7天未結算清單的SQList
                var SQList = _redisHelper.StringGet(redisKey);

                //再一起從TpBetRecord拿過去7天至1小時前的未結算資料
                var unSettleList = _thirdPartyBLL.GetTpBetRecordUnSettleList((int)_tp, startTime, endTime, SQList)
                    .DistinctBy(i => i.SQ)
                    .Where(i => i.SettlementType == SettlementType.UnSettled);

                _logger.LogInformation($"Start: {taskProperty} | SBSW UnSettle Count is : {unSettleList.Count()}");
                foreach (var unSettle in unSettleList.OrderBy(i => i.SettledTime).ThenBy(i => i.TransId))
                {
                    _sabaSiteList[unSettle.SiteId].UpdateBetRecordByTransId(unSettle);
                }

                // 把清單存單Redis
                if (unSettleList.Any())
                {
                    var sqList = unSettleList.Select(i => i.SQ);
                    _redisHelper.StringSet(redisKey, JsonConvert.SerializeObject(sqList));
                }

                _logger.LogInformation($"End: {taskProperty}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SBSW UnSettleWager Exception: {taskProperty}");
            }
            finally
            {
                _timer.Change(TimeSpan.FromHours(1), TimeSpan.Zero);
            }
        }
    }
}
