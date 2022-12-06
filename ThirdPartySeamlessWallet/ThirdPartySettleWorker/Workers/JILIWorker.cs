using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class JILIWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.JILI;

        private Timer timerRngByRound;
        private Timer timerRngBySessionId;
        private Timer timerRngBySessionIdCancel;
        private Timer timerSettleJILIRng;
        private Timer timerSettleJILIFish;
        private Timer timerTransactionCheck;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<JILIWorker> _logger;
        private readonly string _tpName;


        public JILIWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<JILIWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //timerSettleJILIRng = new Timer(DoSettleJILIRngWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerRngByRound = new Timer(DoSettleRngByRoundWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerRngBySessionId = new Timer(DoSettleRngBySessionIdWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerRngBySessionIdCancel = new Timer(DoSettleRngBySessionIdCancelWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleJILIFish = new Timer(DoSettleJILIFishWork, GameType.JILI_Fish, TimeSpan.Zero, TimeSpan.Zero);
            timerTransactionCheck = new Timer(DoTransactionCheckWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRngByRound?.Change(Timeout.Infinite, 0);
            timerRngBySessionId?.Change(Timeout.Infinite, 0);
            timerRngBySessionIdCancel?.Change(Timeout.Infinite, 0);
            timerSettleJILIRng?.Change(Timeout.Infinite, 0);
            timerSettleJILIFish?.Change(Timeout.Infinite, 0);
            timerTransactionCheck?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRngByRoundWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_JILI_Settle_Round_Get";
            var bettransSet = "Service_BettransRng_JILI_Settle_Round_Set";
            string logName = "Settle_BettransRng_JILI_Round";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} Round no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        // round 只會有單筆，但有可能跨月
                        var bettrans = _partitionService.GetUnsettleDataList<SettleData_JILI>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            continue;
                        }

                        var bettransData = bettrans.First();
                        var transId = bettransData.TransId.ToString();
                        decimal stake = bettrans.Sum(x => x.Stake);
                        decimal validBet = bettrans.Sum(x => x.ValidBet);
                        decimal winLostAmount = bettrans.Sum(x => x.WinLostAmount);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, bettransData.Account);

                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = bettransData.Account,
                            SettledTime = bettrans.Last().SettleTime,
                            TransTime = bettransData.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, bettransData.GameId.ToString()),
                            TicketStatus = (TicketStatus)bettransData.TicketStatus,
                        };

                        if (bettrans.Any(x => x.TicketStatus == (int)TicketStatus.Refund))
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        }

                        var tt = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.TransTime).DateTime.ToString("yyyyMM");
                        var st = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.ToString("yyyyMM");

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                        if (result == 1)
                        {
                            var ticketStatus = (int)betRecordInfo.TicketStatus;
                            var settledTime = betRecordInfo.SettledTime;
                            var transTime = betRecordInfo.TransTime;
                            Task.Run(() =>
                            {
                                _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                                _ = _settledBLL.SetSettleFlag_BettransRng_JILI(bettransSet, unsettled.TransId, ticketStatus, settledTime);
                                if (tt != st)
                                {
                                    _ = _settledBLL.SetSettleFlag_BettransRng_JILI(bettransSet, unsettled.TransId, ticketStatus, transTime);
                                }
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerRngByRound.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRngBySessionIdWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_JILI_Settle_SessionId_Get";
            var bettransSet = "Service_BettransRng_JILI_Settle_SessionId_Set";
            string logName = "Settle_BettransRng_JILI_SessionId";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} SessionId no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        // sessionId會有多筆round
                        var bettrans = _partitionService.GetUnsettleDataList<SettleData_JILI>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            continue;
                        }

                        var bettransData = bettrans.First();
                        var transId = bettransData.TransId.ToString();
                        decimal stake = bettrans.Sum(x => x.Stake);
                        decimal validBet = bettrans.Sum(x => x.ValidBet);
                        decimal winLostAmount = bettrans.Sum(x => x.WinLostAmount);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, bettransData.Account);

                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = bettransData.Account,
                            SettledTime = bettrans.Last().SettleTime,
                            TransTime = bettransData.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, bettransData.GameId.ToString()),
                        };

                        var tt = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.TransTime).DateTime.ToString("yyyyMM");
                        var st = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.ToString("yyyyMM");

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                        if (result == 1)
                        {
                            var settledTime = betRecordInfo.SettledTime;
                            var transTime = betRecordInfo.TransTime;
                            Task.Run(() =>
                            {
                                _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                                _ = _settledBLL.SetSettleFlag_BettransRng_JILI(bettransSet, unsettled.TransId, 0, settledTime);
                                if (tt != st)
                                {
                                    _ = _settledBLL.SetSettleFlag_BettransRng_JILI(bettransSet, unsettled.TransId, 0, transTime);
                                }
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerRngBySessionId.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRngBySessionIdCancelWork(object state)
        {
            // 處理單筆下注取消無須結算的注單
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var gameType = (GameType)state;
            var bettransGet = "Service_BettransRng_JILI_SessionId_Get";
            string logName = "Settle_BettransRng_JILI_SessionId_Cancel";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (tpUnsettleList.Any())
                {
                    foreach (var unsettled in tpUnsettleList)
                    {
                        // get date by sessionId
                        var check = _partitionService.Query<CheckBettransData_JILI>(unsettled.PartitionTime, bettransGet, new { sessionId = unsettled.TransId });
                        if (check.Any())
                        {
                            // only one row and cancel remove tpunsettlelist
                            if (check.Count() == 1 && check.Any(x => x.TicketStatus == (int)TicketStatus.Refund))
                            {
                                _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                                _logger.LogWarning($"cancel {unsettled.TransId} no need to settle.", logName);
                            }
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
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex.ToString(), logName);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);
                timerRngBySessionId.Change(dueTimeSpan, TimeSpan.Zero);
            }
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

        private void DoTransactionCheckWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.JILI;
            string logName = "TransactionCheck";
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            var YYYYMM = "202211";

            try
            {
                var checkList = _settledBLL.GetTransactionCheck_JILI(YYYYMM);

                if (checkList.Any())
                {
                    if (checkList != null)
                    {
                        forContinue = checkList.Count() != 0;
                        foreach (var check in checkList)
                        {
                            try
                            {
                                bool isBet = false;
                                var transId = check.TransId.ToString();

                                // check FinancialLog
                                var financialData = _thirdPartyBLL.GetFinancialLogTransactionCheck(transId, YYYYMM);
                                if (financialData.Any())
                                {
                                    if (financialData.Any(x => x.Type == 11032))
                                    {
                                        isBet = true;
                                    }
                                }

                                // check BonusLog
                                if (!isBet)
                                {
                                    var bonusData = _thirdPartyBLL.GetBonusLogTransactionCheck(transId);
                                    if (bonusData.Any())
                                    {
                                        if (bonusData.Any(x => x.Type == 11032))
                                        {
                                            isBet = true;
                                        }
                                    }
                                }

                                // set settleflag -1:check done、-2:abnormal
                                var settleFlag = (isBet) ? -1 : -2;
                                _settledBLL.SetTransactionCheckSettleFlag_JILI(YYYYMM, check.TransId, settleFlag);
                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                _logger.LogError($"{JsonConvert.SerializeObject(check)}\r\n{ex1}", logName);
                            }
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
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(20);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(2);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerTransactionCheck.Change(dueTimeSpan, TimeSpan.Zero);
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
