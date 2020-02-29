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
        public WorkerService(ILogger<WorkerService> logger, IHostEnvironment environment, ThirdPartyBLL thirdPartyBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            WorkerList = new List<IThirdPartyBetHelper>();
            syncParamsList = new ConcurrentDictionary<int, SyncParams>();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            tpList = thirdPartyBLL.GetTpList();
            foreach (var tp in tpList)
            {
                try
                {
                    var TpBetHelper = (IThirdPartyBetHelper)Activator.CreateInstance(Type.GetType($"ThirdPartyBetHistory.Service.TpBetHistory.{tp.TpName}BetHelper"));
                    TpBetHelper.thirdPartyModel = tp;
                    TpBetHelper.syncParams = TpBetHelper.InitialSyncParams();
                    WorkerList.Add(TpBetHelper);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                foreach (var betHelp in WorkerList)
                {
                    var tpBetHelp = betHelp;
                    if (tpBetHelp.syncParams.DoneTime.AddSeconds(tpBetHelp.syncParams.timeOfFrequency) < DateTime.Now && !tpBetHelp.IsRunning)
                    {
                        tpBetHelp.IsRunning = true;
                        _ = Task.Factory.StartNew(async () =>
                          {
                              logger.LogInformation("AddBetRecord at: {time}", DateTimeOffset.Now);
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
                                  logger.LogInformation($"Name: {tpBetHelp.thirdPartyModel.TpName}, key: {tpBetHelp.syncParams.key}");
                              }
                              catch (Exception ex)
                              {
                                  logger.LogError(ex.Message);
                              }
                              finally
                              {
                                  tpBetHelp.IsRunning = false;
                              }
                          });
                    }
                }

                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        private void DoTask()
        {
            try
            {

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
