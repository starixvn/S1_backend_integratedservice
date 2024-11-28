using System.Text.RegularExpressions;
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
    public class SkywindWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.Skywind; }

        private Timer timerRNG;
        private Timer timerLiveCasino;
        private Timer timerFish;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<SkywindWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public SkywindWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<SkywindWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(SettleRNG, GameType.Skywind_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerLiveCasino = new Timer(SettleLiveCasino, GameType.Skywind_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            timerFish = new Timer(SettleFish, GameType.Skywind_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerLiveCasino?.Change(Timeout.Infinite, 0);
            timerFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void SettleRNG(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var logName = $"Settle{gametype}";
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
                        string[] sArray = Regex.Split(tpUnsettle.TransId, "-", RegexOptions.IgnoreCase);
                        var roundId = sArray[0];
                        tpUnsettle.TransId = roundId;
                        //Get wager data
                        var wager = _settledBLL.GetSettle_TpWager_Skywind(tpUnsettle.TransId);
                        if (wager == null)
                        {
                            continue;
                        }
                        //Get all Bettrans data
                        var UnsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_Skywind>("Service_Bettrans_Skywind_Unsettle_Get", tpUnsettle);
                        var settleData = UnsettleData.Where(i => i.TicketType == (int)BettransType.EndRound);
                        var betData = UnsettleData.Where(i => i.TicketType == (int)BettransType.Bet && i.TicketStatus != (int)TicketStatus.Refund);
                        var tpUserName = settleData.FirstOrDefault().custid;
                        var gameId = settleData.FirstOrDefault().gameid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        #region 匯率轉換
                        wager.debit *= tpCurrency.ExchangeRate;
                        wager.Stake *= tpCurrency.ExchangeRate;
                        wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                        wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                        #endregion

                        if (betData.Sum(x => x.amount) == wager.debit || betData.Sum(x => x.amount) == wager.Stake)
                        {
                            var stake = wager.Stake;
                            var winLostAmount = wager.WinLostAmount - stake;
                            var settledTime = wager.SettleTime;
                            var betTime = wager.TransTime;
                            var gameCode = settleData.FirstOrDefault().gamecode;
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = stake,
                                WinlostAmount = winLostAmount,
                                TransId = $"{tpUnsettle.TransId}-{gameId}",
                                GameType = gametype,
                                TpUserName = tpUserName,
                                SettledTime = settledTime,
                                TransTime = betTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameCode),
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : winLostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                                GameID = gameCode,
                                ProgressiveBet = wager.ProgressiveBet,
                                ProgressiveWin = wager.ProgressiveWin,
                            };
                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag("Service_Bettrans_Skywind_Settle_Set", Tp, gametype, $"{tpUnsettle.TransId}-{gameId}", tpUnsettle.TransId, true, DataBase.Wager);
                                });
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(UnsettleData)}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {tpUnsettle.TransId}");
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

        private void SettleLiveCasino(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var logName = $"Settle{gametype}";
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
                        string[] sArray = Regex.Split(tpUnsettle.TransId, "-", RegexOptions.IgnoreCase);
                        var roundId = sArray[0];
                        tpUnsettle.TransId = roundId;
                        //Get wager data
                        var wager = _settledBLL.GetSettle_TpWager_Skywind(tpUnsettle.TransId);
                        if (wager == null)
                        {
                            continue;
                        }
                        //Get all Bettrans data
                        var UnsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_Skywind>("Service_Bettrans_Skywind_Unsettle_Get", tpUnsettle);
                        var settleData = UnsettleData.Where(i => i.TicketType == (int)BettransType.EndRound);
                        var betData = UnsettleData.Where(i => i.TicketType == (int)BettransType.Bet);
                        var tpUserName = settleData.FirstOrDefault().custid;
                        var gameId = settleData.FirstOrDefault().gameid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        #region 匯率轉換
                        wager.debit *= tpCurrency.ExchangeRate;
                        wager.Stake *= tpCurrency.ExchangeRate;
                        wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        #endregion

                        if (betData.Sum(x => x.amount) == wager.debit || betData.Sum(x => x.amount) == wager.Stake)
                        {
                            var stake = wager.Stake;
                            var winLostAmount = wager.WinLostAmount - stake;
                            var settledTime = wager.SettleTime;
                            var betTime = wager.TransTime;
                            var gameCode = settleData.FirstOrDefault().gamecode;
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = stake,
                                WinlostAmount = winLostAmount,
                                TransId = $"{tpUnsettle.TransId}-{gameId}",
                                GameType = gametype,
                                TpUserName = tpUserName,
                                SettledTime = settledTime,
                                TransTime = betTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameCode),
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : winLostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                                GameID = gameCode,
                            };
                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag("Service_Bettrans_Skywind_Settle_Set", Tp, gametype, $"{tpUnsettle.TransId}-{gameId}", tpUnsettle.TransId, true, DataBase.Wager);
                                });
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {tpUnsettle.TransId}");
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerLiveCasino?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void SettleFish(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var logName = $"Settle{gametype}";
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
                        string[] sArray = Regex.Split(tpUnsettle.TransId, "-", RegexOptions.IgnoreCase);
                        var roundId = sArray[0];
                        tpUnsettle.TransId = roundId;
                        //Get wager data
                        var wager = _settledBLL.GetSettle_TpWager_Skywind(tpUnsettle.TransId);
                        if (wager == null)
                        {
                            continue;
                        }
                        //Get all Bettrans data
                        var UnsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_Skywind>("Service_Bettrans_Skywind_Unsettle_Get", tpUnsettle);
                        var settleData = UnsettleData.Where(i => i.TicketType == (int)BettransType.EndRound);
                        var betData = UnsettleData.Where(i => i.TicketType == (int)BettransType.Bet);
                        var tpUserName = settleData.FirstOrDefault().custid;
                        var gameId = settleData.FirstOrDefault().gameid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        #region 匯率轉換
                        wager.debit *= tpCurrency.ExchangeRate;
                        wager.Stake *= tpCurrency.ExchangeRate;
                        wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        #endregion

                        if (betData.Sum(x => x.amount) == wager.debit || betData.Sum(x => x.amount) == wager.Stake)
                        {
                            var stake = wager.Stake;
                            var winLostAmount = wager.WinLostAmount - stake;
                            var settledTime = wager.SettleTime;
                            var betTime = wager.TransTime;
                            var gameCode = settleData.FirstOrDefault().gamecode;
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = stake,
                                WinlostAmount = winLostAmount,
                                TransId = $"{tpUnsettle.TransId}-{gameId}",
                                GameType = gametype,
                                TpUserName = tpUserName,
                                SettledTime = settledTime,
                                TransTime = betTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameCode),
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : winLostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost,
                                GameID = gameCode,
                            };
                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Fishing);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag("Service_Bettrans_Skywind_Settle_Set", Tp, gametype, $"{tpUnsettle.TransId}-{gameId}", tpUnsettle.TransId, true, DataBase.Wager);
                                });
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {tpUnsettle.TransId}");
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerFish?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
