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
                DoTransferTask();
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
                            logger.LogError(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }

        private void DoTransferTask()
        {
            try
            {
                var tpTransferList = tpTransferInfo.TpTransferWaitingGet();

                if (tpTransferList != null)
                {
                    foreach (var tpTransfer in tpTransferList)
                    {
                        try
                        {
                            // set status to processing
                            tpTransferInfo.TpTransferWaitingSet(tpTransfer.TpId, tpTransfer.CustId, (int)tpTransfer.direction, 1);

                            TpInfo tpInfo = new TpInfo
                            {
                                TpId = tpTransfer.TpId,
                                CustId = tpTransfer.CustId,
                            };
                            var tpBalanceData = thirdPartyBLL.GetBalance(tpInfo);

                            tpTransfer.Amount = tpBalanceData.Data.balance;
                            var tpTransferData = thirdPartyBLL.Transfer(tpTransfer);
                            if (tpTransferData.ErrorCode == ErrorCode.Success)
                            {
                                // set status to finished
                                tpTransferInfo.TpTransferWaitingSet(tpTransfer.TpId, tpTransfer.CustId, (int)tpTransfer.direction, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
