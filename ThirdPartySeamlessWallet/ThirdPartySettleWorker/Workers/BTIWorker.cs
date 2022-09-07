using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class BTIWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.BTI;
        private Timer _timerSports;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<BTIWorker> _logger;
        private readonly string _tpName;

        public BTIWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<BTIWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSports = new Timer(DoSettle, GameType.BTI_Sports, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSports?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            var gameType = (GameType)state;
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{gameType} no unsettle data.");
                }

                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettledList = _tpUnSettledBLL.GetTpUnsettleData<BTISettleData>("Service_BettransSports_BTI_Settle_Get", new { reserve_id = transId }, DataBase.Main);
                        var tpUserName = unsettledList.First().Account;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        var stake = unsettledList.Sum(i => i.DebitAmount);
                        var payout = unsettledList.Sum(i => i.CreditAmount);
                        var isClosed = unsettledList.Any(i => i.CurrentStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase));
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = stake,
                            WinlostAmount = payout - stake,
                            TransId = unsettledList.First().BetID,
                            GameType = gameType,
                            TpUserName = tpUserName,
                            SettledTime = unsettledList.Max(i => i.SettleTime),
                            TransTime = unsettledList.Min(i => i.TransTime),
                            SettlementType = isClosed ? SettlementType.SettledForValid : SettlementType.UnSettled,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Sports);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                if (isClosed)
                                {
                                    _tpUnSettledBLL.SetSettleFlag("Service_BettransSports_BTI_Settle_Set_V2", new { account = tpUserName, transId, isSuccess = true });
                                }
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, true);
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"BTI Settle exception. {ex.Message} || TransId : {transId}");
                    }
                }
                
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"BTI Settle exception. {ex.Message}");
            }
            finally
            {
                _timerSports.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
