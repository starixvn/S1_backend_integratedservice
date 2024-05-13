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
    public class APWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.AP; }

        private Timer timerRNG;
        private Timer timerCardRacing;
        private Timer timerCrash;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<APWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;
        private readonly ThirdPartyBLL _thirdPartyBLL;


        public APWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<APWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(Settle, GameType.AP_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerCardRacing = new Timer(DoSettleRngCardRacingWork, GameType.AP_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerCrash = new Timer(DoSettleRngCrashWork, GameType.AP_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerCardRacing?.Change(Timeout.Infinite, 0);
            timerCrash?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void Settle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gametype} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        // mini game
                        if (tpUnsettle.TransId.Contains("_") && tpUnsettle.TransId.Length > 20)
                        {
                            continue;
                        }

                        //Get all Bettrans data
                        var UnsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_AP>("Service_BettransRng_AP_Unsettle_Get", tpUnsettle);
                        var settleData = UnsettleData.Where(i => i.TicketType == (int)BettransType.EndRound);
                        var betData = UnsettleData.Where(i => i.TicketType == (int)BettransType.Bet);

                        var tpUserName = settleData.FirstOrDefault().playerid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var stake = settleData.Sum(x => x.totalstake);
                        var winLostAmount = settleData.Sum(x => x.totalwin) + settleData.Sum(x => x.jpwin) - stake;
                        var settledTime = settleData.FirstOrDefault(x => x.settletime > 0).settletime;
                        var betTime = betData.FirstOrDefault(x => x.bettime > 0).bettime;
                        var gameId = settleData.FirstOrDefault().gamecode;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = stake,
                            WinlostAmount = winLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = settledTime,
                            TransTime = betTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameId),
                            TicketStatus = winLostAmount > 0 ? TicketStatus.Win : winLostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                            GameID = gameId,
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRngCardRacingWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_AP_Mini_cardracing_Settle_Get";
            var bettransSet = "Service_BettransRng_AP_Mini_cardracing_Settle_Set";
            var wagerGet = "Service_TpWagerRng_AP_Mini_Settle_Get";
            var wagerSet = "Service_TpWagerRng_AP_Mini_Settle_Set";
            string logName = "Settle_BettransRng_AP_Mini_cardracing";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} cardracing no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var bettrans = _partitionService.GetUnsettleDataList<SettleData_AdvantPlayMini>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            continue;
                        }

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AdvantPlayMini>(wagerGet, new { TransId = unsettled.TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:wager not found (cardracing)\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var bettransData = bettrans.First();
                        var transId = bettransData.TransId.ToString();
                        decimal stake = bettrans.Sum(x => x.Stake);
                        decimal validBet = bettrans.Sum(x => x.ValidBet);
                        decimal winLostAmount = bettrans.Sum(x => x.WinLostAmount);

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, bettransData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        if (wager.Stake != stake || wager.WinLostAmount != winLostAmount)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:amount not match (cardracing)\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = bettransData.Account,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, bettransData.MiniGameCode.ToLower()),
                            GameID = bettransData.MiniGameCode,
                        };

                        if (wager.status.ToLower() == "refund")
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.SettledTime = wager.RefundTime;
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
                            _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gameType, unsettled.TransId, true, DataBase.Wager);
                            _ = _settledBLL.SetSettleFlag_BettransRng_AP_Mini(bettransSet, unsettled.TransId, settledTime);
                            if (tt != st)
                            {
                                _ = _settledBLL.SetSettleFlag_BettransRng_AP_Mini(bettransSet, unsettled.TransId, transTime);
                            }
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }
                timerCardRacing.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRngCrashWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_AP_Mini_crash_Settle_Get";
            var bettransSet = "Service_BettransRng_AP_Mini_crash_Settle_Set";
            var wagerGet = "Service_TpWagerRng_AP_Mini_Settle_Get";
            var wagerSet = "Service_TpWagerRng_AP_Mini_Settle_Set";
            string logName = "Settle_BettransRng_AP_Mini_crash";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} crash no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var bettrans = _partitionService.GetUnsettleDataList<SettleData_AdvantPlayMini>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            continue;
                        }

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AdvantPlayMini>(wagerGet, new { TransId = unsettled.TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:wager not found (crash)\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var bettransData = bettrans.First();
                        var transId = bettransData.TransId.ToString();
                        decimal stake = bettrans.Sum(x => x.Stake);
                        decimal validBet = bettrans.Sum(x => x.ValidBet);
                        decimal winLostAmount = bettrans.Sum(x => x.WinLostAmount);

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, bettransData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.ValidBet *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        if (wager.Stake != stake || wager.WinLostAmount != winLostAmount)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:amount not match (crash)\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = bettransData.Account,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, bettransData.MiniGameCode.ToLower()),
                            GameID = bettransData.MiniGameCode,
                        };

                        if (wager.status.ToLower() == "refund")
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.SettledTime = wager.RefundTime;
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
                            _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gameType, unsettled.TransId, true, DataBase.Wager);
                            _ = _settledBLL.SetSettleFlag_BettransRng_AP_Mini(bettransSet, unsettled.TransId, settledTime);
                            if (tt != st)
                            {
                                _ = _settledBLL.SetSettleFlag_BettransRng_AP_Mini(bettransSet, unsettled.TransId, transTime);
                            }
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }
                timerCrash.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
