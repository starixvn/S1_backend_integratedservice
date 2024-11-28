using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    class ATWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Iconic;

        private Timer timerRng;
        private Timer timerFish;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<ATWorker> _logger;
        private readonly string _tpName;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public ATWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<ATWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRng = new Timer(DoSettleATRngWork, GameType.Iconic_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerFish = new Timer(DoSettleATFishWork, GameType.Iconic_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRng?.Change(Timeout.Infinite, 0);
            timerFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }


        private void DoSettleATRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Iconic;
            var gameType = GameType.Iconic_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_AT_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            var bettransGet = "Service_BettransRng_Iconic_Settle_Get";
            var wagerGet = "Service_TpWagerRng_AT_Settle_Get";
            var wagerSet = "Service_TpWagerRng_AT_Settle_Set";
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                if (tpUnsettleList != null)
                {
                    forContinue = tpUnsettleList.Count() == 200;
                    foreach (var unsettled in tpUnsettleList)
                    {
                        try
                        {
                            string TransId = unsettled.TransId;
                            var bettrans = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(bettransGet, new { _TransId = TransId }, DataBase.Main).First();
                            var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AT>(wagerGet, new { _TransId = TransId }, DataBase.Wager).First();
                            if (bettrans.TransId == null && wager.Status == "cancel")
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, TransId, isSuccess: true);
                                });
                                LogContext.PushProperty("TP", tpName);
                                _logger.LogWarning($"{logName}:wager cancel no bettrans data\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                                continue;
                            }

                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, bettrans.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate != 0)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.ValidBet *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            if (wager.Stake != bettrans.Stake || wager.WinLostAmount != bettrans.WinLostAmount)
                            {
                                LogContext.PushProperty("TP", tpName);
                                _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                                continue;
                            }

                            decimal stake = bettrans.Stake;
                            decimal validBet = wager.ValidBet;
                            decimal winLostAmount = bettrans.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = TransId,
                                GameType = gameType,
                                TpUserName = bettrans.Account,
                                SettledTime = wager.SettleTime,
                                TransTime = wager.TransTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, bettrans.GameId.ToString()),
                                GameID = bettrans.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gameType, TransId, true, DataBase.Wager);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                            });
                            isError = true;
                            _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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
                _logger.LogError(ex,$"{logName}:{ex.Message}");
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleATFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Iconic;
            var gameType = GameType.Iconic_Fish;
            var category = TpCategory.Fishing;
            string logName = "Settle_AT_Fish";
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            var wagerGet = "Service_TpWagerFish_AT_Settle_Get";
            var wagerSet = "Service_TpWagerFish_AT_Settle_Set";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                if (tpUnsettleList != null)
                {
                    forContinue = tpUnsettleList.Count() == 200;
                    foreach (var unsettled in tpUnsettleList)
                    {
                        try
                        {
                            string TransId = unsettled.TransId;
                            var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_AT>(wagerGet, new { _TransId = TransId }, DataBase.Wager).First();

                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, wager.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate != 0)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.ValidBet *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            decimal stake = wager.Stake;
                            decimal validBet = wager.ValidBet;
                            decimal winLostAmount = wager.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = TransId,
                                GameType = gameType,
                                TpUserName = wager.Account,
                                SettledTime = wager.SettleTime,
                                TransTime = wager.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = wager.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gameType, TransId, true, DataBase.Wager);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                            });
                            isError = true;
                            _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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
                _logger.LogError(ex, $"{logName}:{ex.Message}");
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerFish.Change(dueTimeSpan, TimeSpan.Zero);
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
