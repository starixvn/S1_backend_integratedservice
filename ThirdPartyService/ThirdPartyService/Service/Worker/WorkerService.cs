using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.Worker
{
    public class WorkerService : BackgroundService
    {
        private Timer _timer;
        private Timer _bonusTimer;
        private Timer _remainingBonusTimer;
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
            _remainingBonusTimer = new Timer(DoTaskRemainingBonusAmountTransfer, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        /// <summary>
        /// TpTransfer check worker
        /// </summary>
        /// <param name="state"></param>
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
                            _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(5);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        /// <summary>
        /// pending bonus transfer check worker
        /// </summary>
        /// <param name="state"></param>
        private void DoTaskForBonusTransfer(object state)
        {
            try
            {
                var tpTransferList = _tpTransferInfo.TpBonusTransferPendingGet();
                var statusList = new List<int> { (int)TransferStatus.Pending, (int)TransferStatus.PendingD };
                
                if (tpTransferList != null)
                {
                    foreach (var tpTransfer in tpTransferList)
                    {
                        var pendingTpBonusTransfer =
                            _tpTransferInfo.TpBonusTransferInfoPendingGet(tpTransfer.CustId,
                                JsonConvert.SerializeObject(statusList));
                        try
                        {
                            if (pendingTpBonusTransfer != null)
                            {
                                _thirdPartyBll.BonusCheckTransfer(pendingTpBonusTransfer);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                _bonusTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Lose out , Remaining bonus transfer worker
        /// </summary>
        /// <param name="state"></param>
        private void DoTaskRemainingBonusAmountTransfer(object state)
        {
            try
            {
                var tpTransferList = _tpTransferInfo.TpRemainingTransferBonusGet();
                foreach (var tpTransfer in tpTransferList)
                {
                    var tpBonusCancelTransfer = new TpBonusCancelTransfer
                    {
                        TpId = tpTransfer.TpId,
                        CustId = tpTransfer.CustId,
                        UserName = tpTransfer.UserName,
                        SiteId = tpTransfer.SiteId,
                        PromoCategoryId = tpTransfer.PromoCategoryId,
                        BonusId = tpTransfer.BonusId,
                        RemainingBonus = tpTransfer.RemainingAmount,
                        RefundAmount = 0,
                        UpdateBonusTransferStatus = true
                    };
                    _thirdPartyBll.BonusCancelTransfer(tpBonusCancelTransfer);
                }
            }catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                _remainingBonusTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
