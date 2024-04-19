using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class CQ9Worker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.CQ9;
        private Timer _timerSettleCq9Slot;
        private Timer _timerSettleCq9Fish;
        private Timer _timerSettleCq9WCC;

        private readonly SettledBLL _settledBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly ILogger<CQ9Worker> _logger;

        public CQ9Worker(IServiceProvider provider)
        {
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<CQ9Worker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleCq9Slot = new Timer(DoSettleCQ9SlotWork, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerSettleCq9Fish = new Timer(DoSettleCQ9FishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerSettleCq9WCC = new Timer(DoSettleCQ9WCCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleCq9Slot?.Change(Timeout.Infinite, 0);
            _timerSettleCq9Fish?.Change(Timeout.Infinite, 0);
            _timerSettleCq9WCC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        
        private void DoSettleCQ9SlotWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.CQ9;
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            var gameType = GameType.CQ9_RNG;
            
            try
            {
                var unsettledList = _tpUnSettledBll.GetTpUnsettleList(Tp, gameType);
                
                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var unsettledData = _settledBll.GetUnsettledListRNG_CQ9(unsettled.TransId);
                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache(thisTp, unsettledData.Account);

                            var stake = unsettledData.Stake;
                            var validBet = unsettledData.Stake;
                            var winLostAmount = unsettledData.WinLostAmount;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettledData.roundid,
                                GameType = GameType.CQ9_RNG,
                                TpUserName = unsettledData.Account,
                                SettledTime = unsettledData.SettleTime,
                                TransTime = unsettledData.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = unsettledData.GameId,
                            };

                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            Task.Run(() =>
                            {
                                _settledBll.SetSettleFlagRNG_CQ9(unsettledData.roundid, 1);
                                _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettledData.roundid, isSuccess: true);
                            });
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError(ex1,$"SettleCQ9:{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex, $"SettleCQ9:{ex.Message}");
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
                var unsettledList = _tpUnSettledBll.GetTpUnsettleList(Tp, gameType);
                
                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var unsettledData = _settledBll.GetUnsettledListFish_CQ9(unsettled.TransId);
                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache(thisTp, unsettledData.Account);

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettledData.Stake,
                                ValidBet = unsettledData.Stake,
                                WinlostAmount = unsettledData.WinLostAmount,
                                TransId = unsettledData.roundid,
                                GameType = gameType,
                                TpUserName = unsettledData.Account,
                                SettledTime = unsettledData.SettleTime,
                                TransTime = unsettledData.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = unsettledData.GameId
                            };

                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, category);

                            Task.Run(() =>
                            {
                                _settledBll.SetSettleFlagFish_CQ9(unsettledData.roundid, 1);
                                _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettledData.roundid, isSuccess: true);
                            });
                            
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
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
        
        private void DoSettleCQ9WCCWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.CQ9;
            var gameType = GameType.CQ9_WCC;
            var category = TpCategory.Cockfighting;
            var logName = "Settle_CQ9_WCC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                //var unsettledList = _settledBll.GetUnsettledListWCC_CQ9();
                var unsettledList = new List<SettleDataRNG_CQ9>();
                
                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache(thisTp, unsettled.Account);

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
                                SettlementType = SettlementType.SettledForValid,
                                GameID = unsettled.GameId
                            };

                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, category);

                            _settledBll.SetSettleFlagWCC_CQ9(unsettled.roundid, 1);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
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

                _timerSettleCq9WCC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
