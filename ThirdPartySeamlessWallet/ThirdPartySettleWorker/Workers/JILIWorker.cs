using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class JILIWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.JILI;

        private Timer timerSettleJILIRng;
        private Timer timerSettleJILIFish;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<JILIWorker> _logger;
        private readonly string _tpName;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public JILIWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<JILIWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleJILIRng = new Timer(DoSettleJILIRngWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleJILIFish = new Timer(DoSettleJILIFishWork, GameType.JILI_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleJILIRng?.Change(Timeout.Infinite, 0);
            timerSettleJILIFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleJILIRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.JILI;
            var gameType = GameType.JILI_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_JILI_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledList_JILI((int)category);


                if (!unsettledList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType.ToString()} no unsettle data.");
                }

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId.ToString()),
                                TicketStatus = (TicketStatus)unsettled.TicketStatus,
                            };
                            if (unsettled.TicketStatus == (int)TicketStatus.Refund)
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            }
                            _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            _settledBLL.SetSettleFlag_JILI((int)category, unsettled.TransId, unsettled.TicketStatus);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerSettleJILIRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleJILIFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.JILI;
            var gameType = GameType.JILI_Fish;
            var category = TpCategory.Fish;
            string logName = "Settle_JILI_Fish";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledList_JILI((int)category);

                if (!unsettledList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType.ToString()} no unsettle data.");
                }

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = unsettled.TicketStatus == (int)TicketStatus.Refund ? SettlementType.SettledForInvalid : SettlementType.SettledForValid,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus
                            };

                            _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            _settledBLL.SetSettleFlag_JILI((int)category, unsettled.TransId, unsettled.TicketStatus);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerSettleJILIFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
        {
            TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(1);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromMinutes(1);
            }

            return dueTimeSpan;
        }
    }
}
