using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.Worker
{
    public class WorkerService : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<WorkerService> logger;
        private readonly TpTransferInfo tpTransferInfo;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public WorkerService(ILogger<WorkerService> logger, IHostEnvironment environment, TpTransferInfo tpTransferInfo, ThirdPartyBLL thirdPartyBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.tpTransferInfo = tpTransferInfo;
            this.thirdPartyBLL = thirdPartyBLL;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                DoTask();
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(30000, stoppingToken);
            }
        }

        private void DoTask()
        {
            try
            {
                var tpTransferList = tpTransferInfo.TpTransferPendingGet();

                if (tpTransferList != null)
                {
                    foreach (var tpTransfer in tpTransferList)
                    {
                        try
                        {
                            thirdPartyBLL.CheckTransfer(tpTransfer);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }
}
