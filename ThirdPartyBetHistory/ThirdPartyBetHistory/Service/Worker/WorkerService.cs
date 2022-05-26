using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class WorkerService : BackgroundService
    {
        private readonly ILogger<WorkerService> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IEnumerable<IThirdPartyBetHelper> _sabaSiteList;
        private readonly Dictionary<SiteId, Timer> timerWorkerList;
        public WorkerService(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WorkerService>>();
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
                    _logger.LogError(ex, "Error: Worker Service");
                }
            }

            return Task.CompletedTask;
        }
        private void DoTask(object state)
        {
            var tpBetHelp = (IThirdPartyBetHelper)state;
            var taskProperty = $"Name:{tpBetHelp.thirdPartyModel.TpName} ,SiteId:{tpBetHelp.SiteId} , key:{tpBetHelp.syncParams.key}";

            try
            {
                if (tpBetHelp.syncParams.DoneTime.AddSeconds(tpBetHelp.syncParams.timeOfFrequency) < DateTime.Now && !tpBetHelp.IsRunning)
                {
                    tpBetHelp.IsRunning = true;
                    _ = Task.Factory.StartNew(async () =>
                    {
                        LogContext.PushProperty("WorkerId", Guid.NewGuid());
                        _logger.LogDebug("AddBetRecord at: {time}", DateTimeOffset.Now);
                        try
                        {
                            await tpBetHelp.AddBetRecord((betRecordInfos) =>
                            {
                                foreach (var betRecord in betRecordInfos)
                                {
                                    betRecord.TpId = tpBetHelp.thirdPartyModel.Id;
                                }

                                _thirdPartyBLL.ToDoAddBetRecord(betRecordInfos);
                                if (betRecordInfos.Count() > 50)
                                    tpBetHelp.syncParams.DoneTime = DateTime.Now.AddSeconds(-tpBetHelp.syncParams.timeOfFrequency);
                            });

                            if (tpBetHelp.SiteId == SiteId.Nova88)
                            {
                                _thirdPartyBLL.SetTicketKey(tpBetHelp.thirdPartyModel.Id, tpBetHelp.syncParams.key);
                            }
                            else
                            {
                                _thirdPartyBLL.SiteTpTicketKey(tpBetHelp.thirdPartyModel.Id, tpBetHelp.thirdPartyModel.SiteId, tpBetHelp.syncParams.key);
                            }

                            tpBetHelp.syncParams.DoneTime = DateTime.Now;
                            _logger.LogInformation($"Success: {taskProperty}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"StartNew Exception: {taskProperty}");
                        }
                        finally
                        {
                            tpBetHelp.IsRunning = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DoTask Exception: {taskProperty}");
            }
            finally
            {
                //tpBetHelp
                timerWorkerList[tpBetHelp.SiteId].Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
            }
        }
    }
}
