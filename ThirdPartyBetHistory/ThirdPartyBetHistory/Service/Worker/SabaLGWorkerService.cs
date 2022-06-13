using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaLGWorkerService : BackgroundService
    {
        private readonly ILogger<SabaLGWorkerService> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<SiteId, Timer> timerWorkerList;
        private readonly WorkerType workerType = WorkerType.LGVirtualSportsWager;

        public SabaLGWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaLGWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            timerWorkerList = new Dictionary<SiteId, Timer>();
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
                    _logger.LogError(ex, "Error: SabaLGWorkerService");
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
         
            var lastTicketKey = _thirdPartyBLL.GetSiteTicketKey(workerType, tpBetHelp);
            var taskProperty = $"{tpBetHelp.thirdPartyModel.TpName}:{workerType} | SiteId:{tpBetHelp.SiteId} | siteTicketKey:{lastTicketKey}";
            try
            {          
                var nextTicketKey = tpBetHelp.GetVS3rdBetDetail(workerType,lastTicketKey).Result;
                _thirdPartyBLL.SetSiteTicketKey(workerType, tpBetHelp, nextTicketKey);
                _logger.LogInformation($"Success: {taskProperty} at: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm.ss.fff}");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
            }
        }
    }
}
