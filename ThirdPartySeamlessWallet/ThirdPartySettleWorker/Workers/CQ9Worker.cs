using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class CQ9Worker:BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.CQ9;
        private Timer _timerSettleCq9Slot;
        private Timer _timerSettleCq9Fish;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<CQ9Worker> _logger;
        
        public CQ9Worker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<CQ9Worker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleCq9Slot = new Timer(DoSettleCQ9SlotWork, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerSettleCq9Fish = new Timer(DoSettleCQ9FishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleCq9Slot?.Change(Timeout.Infinite, 0);
            _timerSettleCq9Fish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }


        private void DoSettleCQ9SlotWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.CQ9;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListRNG_CQ9();

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

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.roundid,
                                GameType = GameType.CQ9_RNG,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            Task.Run(() =>
                            {
                                _settledBLL.SetSettleFlagRNG_CQ9(unsettled.roundid, 1);
                            });
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleCQ9");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), "SettleCQ9");
                isError = true;
            }
            finally
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

                _timerSettleCq9Slot.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleCQ9FishWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.CQ9;
            var gameType = GameType.CQ9_Fish;
            var category = TpCategory.Fish;
            var logName = "Settle_CQ9_Fish";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListFish_CQ9();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.Stake,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.roundid,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                            _settledBLL.SetSettleFlagFish_CQ9(unsettled.roundid, 1);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName, ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName, ex);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _timerSettleCq9Fish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
