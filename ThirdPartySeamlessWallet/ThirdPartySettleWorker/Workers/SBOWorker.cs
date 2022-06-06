using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class SBOWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.SBO;
        private Timer _timerSports;
        private Timer _timerVirtualSports;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<SBOWorker> _logger;
        private readonly string _tpName;

        public SBOWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<SBOWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSports = new Timer(DoSettle, GameType.SBO_Sports, TimeSpan.Zero, TimeSpan.Zero);
            _timerVirtualSports = new Timer(DoSettle, GameType.SBO_VirtualSports, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSports?.Change(Timeout.Infinite, 0);
            _timerVirtualSports?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var tpCategory = TpCategory.Sports; // SBO_Sports、SBO_VirtualSports 都是體育分類

            const string logName = "SettleSBO";
            const string getSpName = "Service_BettransSports_SBO_Unsettle_Get"; // 取得未結算的注單
            const string getWagerSpName = "Service_TpWagerSports_SBO_Unsettle_Get"; // 取得未結算的注單

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSpName, new { TransId = transId }, DataBase.Main).First();
                        var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getWagerSpName, new { TransId = transId }, DataBase.Wager).First();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, settleData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        
                        if (!IsBettransSameAsTpWager(settleData, tpWagerData, tpCurrency))
                        {
                            _logger.LogError($"transId: {transId} is not equal");
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = settleData.Stake,
                            ValidBet = settleData.Stake,
                            WinlostAmount = settleData.WinLostAmount,
                            TransId = settleData.TransId,
                            GameType = gameType,
                            TpUserName = settleData.Account,
                            SettledTime = settleData.SettleTime,
                            TransTime = settleData.TransTime,
                            SettlementType = SetSettlementType((TicketStatus)settleData.TicketStatus), // 判斷 TicketStatus，因為可能重新結算
                            TicketStatus = SetTicketStatus(settleData.WinLostAmount),
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {transId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{logName} exMessage:{ex.Message}");
                isError = true;
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                _timerSports?.Change(dueTimeSpan, TimeSpan.Zero);
                _timerVirtualSports?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private TicketStatus SetTicketStatus(decimal winLostAmount)
        {
            if (winLostAmount == 0) return TicketStatus.Draw;

            return winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost;
        }

        private SettlementType SetSettlementType(TicketStatus ticketStatus)
        {
            var settlementType = SettlementType.UnSettled;

            switch (ticketStatus)
            {
                case TicketStatus.UnSettled:
                    settlementType = SettlementType.UnSettled;
                    break;
                case TicketStatus.Win:
                case TicketStatus.Lost:
                case TicketStatus.Draw:
                    settlementType = SettlementType.SettledForValid;
                    break;
                case TicketStatus.Refund:
                    settlementType = SettlementType.SettledForInvalid;
                    break;
            }

            return settlementType;
        }

        private bool IsBettransSameAsTpWager(SettleData bettrans, SettleData tpWager, TpCurrency tpCurrency)
        {
            if (bettrans == null || tpWager == null || tpCurrency.ExchangeRate <= 0) return false;
            tpWager.Stake *= tpCurrency.ExchangeRate;
            tpWager.WinLostAmount *= tpCurrency.ExchangeRate;
            
            if (bettrans.TicketStatus == (int)SettlementType.UnSettled || 
                bettrans.TicketStatus == (int)TicketStatus.Refund)
            {
                tpWager.WinLostAmount -= tpWager.Stake;
            }
            
            return bettrans.Stake == tpWager.Stake && bettrans.WinLostAmount == tpWager.WinLostAmount;
        }
    }
}
