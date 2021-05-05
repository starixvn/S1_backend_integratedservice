using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class WorkerService : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<WorkerService> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private IEnumerable<ThirdPartyModel> tpList;
        private List<IThirdPartyBetHelper> WorkerList;
        private ConcurrentDictionary<int, SyncParams> syncParamsList;
        private Dictionary<int, Timer> timerWorkerList;
        public WorkerService(ILogger<WorkerService> logger, IHostEnvironment environment, ThirdPartyBLL thirdPartyBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            WorkerList = new List<IThirdPartyBetHelper>();
            syncParamsList = new ConcurrentDictionary<int, SyncParams>();
            timerWorkerList = new Dictionary<int, Timer>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            tpList = thirdPartyBLL.GetTpList();
            foreach (var tp in tpList.Where(item => item.Id == 1))
            {
                try
                {
                    var TpBetHelper = (IThirdPartyBetHelper)Activator.CreateInstance(Type.GetType($"ThirdPartyBetHistory.Service.TpBetHistory.{tp.TpName}BetHelper"));
                    TpBetHelper.thirdPartyModel = tp;
                    TpBetHelper.syncParams = TpBetHelper.InitialSyncParams();
                    var timer = new Timer(DoTask, TpBetHelper, TimeSpan.Zero, TimeSpan.Zero);
                    timerWorkerList.Add(TpBetHelper.thirdPartyModel.SiteId, timer);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return Task.CompletedTask;
        }
        private void DoTask(object state)
        {
            var tpBetHelp = (IThirdPartyBetHelper)state;

            try
            {
                if (tpBetHelp.syncParams.DoneTime.AddSeconds(tpBetHelp.syncParams.timeOfFrequency) < DateTime.Now && !tpBetHelp.IsRunning)
                {
                    tpBetHelp.IsRunning = true;
                    _ = Task.Factory.StartNew(async () =>
                    {
                        logger.LogTrace("AddBetRecord at: {time}", DateTimeOffset.Now);
                        try
                        {
                            await tpBetHelp.AddBetRecord((betRecordInfos) =>
                            {
                                foreach (var betRecord in betRecordInfos)
                                {
                                    betRecord.TpId = tpBetHelp.thirdPartyModel.Id;
                                }

                                thirdPartyBLL.ToDoAddBetRecord(betRecordInfos);
                                if (betRecordInfos.Count() > 50)
                                    tpBetHelp.syncParams.DoneTime = DateTime.Now.AddSeconds(-tpBetHelp.syncParams.timeOfFrequency);
                            });

                            if (tpBetHelp.thirdPartyModel.SiteId == 0)
                            {
                                thirdPartyBLL.SetTicketKey(tpBetHelp.thirdPartyModel.Id, tpBetHelp.syncParams.key);
                            }
                            else
                            {
                                thirdPartyBLL.SiteTpTicketKey(tpBetHelp.thirdPartyModel.Id, tpBetHelp.thirdPartyModel.SiteId, tpBetHelp.syncParams.key);
                            }

                            tpBetHelp.syncParams.DoneTime = DateTime.Now;
                            logger.LogInformation($"Name: {tpBetHelp.thirdPartyModel.TpName} ,SiteId:{tpBetHelp.thirdPartyModel.SiteId} , key: {tpBetHelp.syncParams.key}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"{ex}");
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
                logger.LogError(ex.Message);
            }
            finally
            {
                //tpBetHelp
                timerWorkerList[tpBetHelp.thirdPartyModel.SiteId].Change(TimeSpan.FromSeconds(1), TimeSpan.Zero);
            }
        }
    }
}
