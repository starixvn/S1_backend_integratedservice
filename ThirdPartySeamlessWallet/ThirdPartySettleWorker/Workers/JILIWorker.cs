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
            timerRngByRound = new Timer(DoSettleRngByRoundWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerRngBySessionId = new Timer(DoSettleRngBySessionIdWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerRngBySessionIdCancel = new Timer(DoSettleRngBySessionIdCancelWork, GameType.JILI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRngByRound?.Change(Timeout.Infinite, 0);
            timerRngBySessionId?.Change(Timeout.Infinite, 0);
            timerRngBySessionIdCancel?.Change(Timeout.Infinite, 0);
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
                            GameID = bettransData.GameId
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
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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
                _logger.LogError(ex, $"{logName}:{ex.Message}");
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
                            GameID = bettransData.GameId,
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
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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
                _logger.LogError(ex, $"{logName}:{ex.Message}");
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
                                _logger.LogWarning($"{logName}:cancel {unsettled.TransId} no need to settle.");
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
                _logger.LogError(ex, $"{logName}:{ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);
                timerRngBySessionId.Change(dueTimeSpan, TimeSpan.Zero);
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
