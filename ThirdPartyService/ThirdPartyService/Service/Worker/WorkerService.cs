using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.Worker
{
    public class WorkerService : BackgroundService
    {
        private Timer _timer;
        private Timer _bonusTimer;
        private readonly ILogger<WorkerService> _logger;
        private readonly TpTransferInfo _tpTransferInfo;
        private readonly ThirdPartyBLL _thirdPartyBll;

        public WorkerService(ILogger<WorkerService> logger,TpTransferInfo tpTransferInfo, ThirdPartyBLL thirdPartyBll)
        {
            _logger = logger;
            _tpTransferInfo = tpTransferInfo;
            _thirdPartyBll = thirdPartyBll;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            _bonusTimer = new Timer(DoTaskForBonusTransfer, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoTask(object state)
        {
            try
            {
                var tpTransferList = _tpTransferInfo.TpTransferPendingGet();
        
                if (tpTransferList != null)
                {
                    foreach (var tpTransfer in tpTransferList)
                    {
                        try
                        {
                            _thirdPartyBll.CheckTransfer(tpTransfer);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoTaskForBonusTransfer(object state)
        {
            try
            {
                var tpTransferList = _tpTransferInfo.TpBonusTransferPendingGet();
                
                if (tpTransferList != null)
                {
                    foreach (var tpTransfer in tpTransferList)
                    {
                        var pendingTpBonusTransfer = _tpTransferInfo.TpBonusTransferInfoPendingGet(tpTransfer.CustId ,  tpTransfer.Bonus);
                        try
                        {
                            if (pendingTpBonusTransfer != null)
                            {
                                _thirdPartyBll.BonusCheckTransfer(pendingTpBonusTransfer);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                _bonusTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
