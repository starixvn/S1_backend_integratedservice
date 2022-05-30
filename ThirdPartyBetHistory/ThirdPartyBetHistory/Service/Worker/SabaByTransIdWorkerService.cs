using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class SabaByTransIdWorkerService : BackgroundService
    {
        private readonly ILogger<SabaByTransIdWorkerService> _logger;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<SiteId, Timer> _timerWorkerList;
        private const WorkerType WorkerType = Model.enums.WorkerType.ByTransIdWager;

        public SabaByTransIdWorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<SabaByTransIdWorkerService>>();
            _sabaSiteList = provider.GetServices<IThirdPartyBetHelper>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
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
                    Thread.Sleep(60000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error: SabaByTransIdWorkerService");
                }
            }

            return Task.CompletedTask;
        }
        
        private void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid());
            var tpBetHelp = (IThirdPartyBetHelper)state;
            var taskProperty = $"{WorkerType}:{tpBetHelp.thirdPartyModel.TpName}, SiteId:{tpBetHelp.SiteId}";
            tpBetHelp.thirdPartyModel.SiteId = (int)tpBetHelp.SiteId;

            try
            {
                tpBetHelp.UpdateBetRecordByTransId((betRecordInfos) =>
                {
                    foreach (var betRecord in betRecordInfos)
                    {
                        betRecord.TpId = tpBetHelp.thirdPartyModel.Id;
                    }

                    _thirdPartyBll.ToDoAddBetRecord(betRecordInfos);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                _timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromHours(4), TimeSpan.Zero);
            }
        }
    }
}
