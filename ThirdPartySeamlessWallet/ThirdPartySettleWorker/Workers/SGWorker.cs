using Newtonsoft.Json;
using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class SGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.SG; }

        private Timer timerRNG;
        private Timer timerTable;
        private Timer timerFish;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<SGWorker> _logger;
        private readonly string _tpName;


        public SGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<SGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(DoSettleRNG, GameType.SG_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerTable = new Timer(DoSettleTable, GameType.SG_Table, TimeSpan.Zero, TimeSpan.Zero);
            timerFish = new Timer(DoSettleFish, GameType.SG_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerTable?.Change(Timeout.Infinite, 0);
            timerFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRNG(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var gameType = (GameType)state;
            var tpCategory = TpCategory.Slots;
            var logName = $"Settle_SG_Rng";
            var getSP = $"Service_BettransRng_SG_Settle_Get_V3";
            var setSP = $"Service_TpWagerRng_SG_Settle_Set_V3";
            var dueTimeSpan = TimeSpan.FromSeconds(30);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { _TransId = transId }, DataBase.Main).First();
                        var wager = _settledBLL.GetSettle_TpWagerRng_SG(unsettled.TransId);
                        if (wager == null)
                        {
                            continue;
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                            wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                        }

                        if (SettleAmountCheck(unsettled, wager))
                        {
                            var stake = unsettled.Stake;
                            var validBet = unsettled.Stake;
                            var winLostAmount = unsettled.WinLostAmount;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                ProgressiveBet = wager.ProgressiveBet,
                                ProgressiveWin = wager.ProgressiveWin,
                                GameID = wager.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, transId, true, DataBase.Wager);
                                });
                            }
                        }
                        else
                        {
                            _logger.LogError($" WinLostAmount || wager {Math.Round(wager.WinLostAmount, 2)}, bettrans {unsettled.WinLostAmount}\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
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
            }
            finally
            {
                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleTable(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var gameType = (GameType)state;
            var tpCategory = TpCategory.P2P;
            var logName = $"Settle_SG_Table";
            var getSP = $"Service_BettransRng_SG_Settle_Get_V3";
            var setSP = $"Service_TpWagerRng_SG_Settle_Set_V3";
            var dueTimeSpan = TimeSpan.FromSeconds(30);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { _TransId = transId }, DataBase.Main).First();
                        var wager = _settledBLL.GetSettle_TpWagerRng_SG(unsettled.TransId);
                        if (wager == null)
                        {
                            continue;
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                            wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                        }

                        if (SettleAmountCheck(unsettled, wager))
                        {
                            // can't get the actually bet amount , so use winLoseAmount
                            var stake = Math.Abs(unsettled.WinLostAmount);
                            var validBet = Math.Abs(unsettled.WinLostAmount);
                            var winLostAmount = unsettled.WinLostAmount;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                ProgressiveBet = wager.ProgressiveBet,
                                ProgressiveWin = wager.ProgressiveWin,
                                GameID = wager.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, transId, true, DataBase.Wager);
                                });
                            }
                        }
                        else
                        {
                            _logger.LogError($" WinLostAmount || wager {Math.Round(wager.WinLostAmount, 2)}, bettrans {unsettled.WinLostAmount} \r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
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
            }
            finally
            {
                timerTable?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleFish(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var gameType = (GameType)state;
            var tpCategory = TpCategory.Fishing;
            var logName = $"Settle_SG_Fish";
            var getSP = $"Service_TpWagerFish_SG_Settle_Get_V3";
            var setSP = $"Service_TpWagerFish_SG_Settle_Set_V3";
            var dueTimeSpan = TimeSpan.FromSeconds(30);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { TransId = transId }, DataBase.Wager).First();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.Stake,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.SettleTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = unsettled.GameId,
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, transId, true, DataBase.Wager);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
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
            }
            finally
            {
                timerFish?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private bool SettleAmountCheck(SettleData unsettled, SettleData_SG wager)
        {
            if (wager.GameId.StartsWith("C-", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var dDiffWinLost = Math.Abs(Math.Round(wager.WinLostAmount, 2)) - Math.Abs(unsettled.WinLostAmount);
            return wager.Stake == unsettled.Stake && Math.Abs(dDiffWinLost) <= 0.2m;
        }
    }
}
