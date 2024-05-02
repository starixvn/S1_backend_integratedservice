using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class IMWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.IM;
        private Timer timerSport;
        private Timer timerESport;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly IIMBLL _imBll;
        private readonly ILogger<IMWorker> _logger;
        private readonly string _tpName;
        private readonly MySqlClient _mainDbClient;

        public IMWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _imBll = provider.GetRequiredService<IIMBLL>();
            _logger = provider.GetRequiredService<ILogger<IMWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSport = new Timer(DoSettleSportWork, GameType.IM_Sports, TimeSpan.Zero, TimeSpan.Zero);
            timerESport = new Timer(DoSettleESportWork, GameType.IM_ESports, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSport?.Change(Timeout.Infinite, 0);
            timerESport?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleSportWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Sports;
            var betTransGet = "Service_BettransSports_IM_Settle_BetId_Get";
            var betTransSet = "Service_BettransSports_IM_Settle_BetId_Set";
            var tpWagerSet = "Service_TpWagerSports_IM_Settle_Set";
            var logName = "IMDoSettleSportsWork";

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
                        var settleBetTrans = _mainDbClient.Query<SettleTransactions_IMSport>(betTransGet, parameters: new { unsettled.TransId });
                        var betDetail = _mainDbClient.Query<SettleTransactions_IMSport>("Service_BettransSport_IM_Bet_Get", parameters: new { _BetId = unsettled.TransId }).FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, betDetail.PlayerId);
                        var transId = betDetail.BetId;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = betDetail.Amount,
                            ValidBet = betDetail.Amount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = betDetail.PlayerId,
                            SettlementType = SettlementType.UnSettled,
                            TicketStatus = TicketStatus.UnSettled,
                            GameID = betDetail.GameId
                        };

                        if (settleBetTrans.Any())
                        {
                            // 最新一筆的Settle結果
                            var lastBetTransData = settleBetTrans.MaxBy(item => item.TimeStamp);

                            //多次結算結果的Sum為payoutAmount總額
                            var payoutAmount = settleBetTrans.Sum(x => x.PayoutAmount);
                            var stake = lastBetTransData.Stake;
                            var winLostAmount = payoutAmount - stake;

                            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                            if (tpCurrency == null)
                            {
                                _logger.LogWarning($"{logName} {lastBetTransData.PlayerId} TpCurrency not found.");
                                continue;
                            }

                            var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<WagerDataIM>("Service_TpWagerSports_IM_Unsettle_Get", new { BetId = transId }, DataBase.Wager).FirstOrDefault();

                            betRecordInfo.WinlostAmount = winLostAmount;
                            betRecordInfo.SettledTime = new DateTimeOffset(lastBetTransData.TimeStamp).ToUnixTimeSeconds();
                            betRecordInfo.TransTime = new DateTimeOffset(tpWagerData.WagerCreationDateTime).ToUnixTimeSeconds();

                            CalculateTicketStatusAndAmount(betRecordInfo, tpWagerData, lastBetTransData.ActionId, out var isRefund);

                            if (tpWagerData.IsSettled == 1)
                            {
                                var settleAmountDataIm = new SettleAmountDataIM
                                {
                                    Stake = betRecordInfo.Stake,
                                    ValidBet = betRecordInfo.ValidBet,
                                    WinLostAmount = betRecordInfo.WinlostAmount
                                };
                                if (!IsBetTransSameAsTpWager(settleAmountDataIm, tpWagerData, tpCurrency, isRefund))
                                {
                                    _logger.LogWarning($"transId: {transId} is not equal " +
                                                     $"settle: {JsonConvert.SerializeObject(settleAmountDataIm)} " +
                                                     $"wager: {JsonConvert.SerializeObject(tpWagerData)}");
                                    continue;
                                }
                            }
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, isSuccess: true);
                                _ = _imBll.SetSettleFlag_Bettrans_IM(betTransSet, transId);
                                _ = _imBll.SetSettleFlag_TpWager_IM(tpWagerSet, transId);
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
                        _logger.LogError(ex1,$"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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
                timerSport.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleESportWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Esports;
            var settleBetTransGet = "Service_BettransESports_IM_Settle_BetId_Get";
            var settleBetTransSet = "Service_BettransESports_IM_Settle_BetId_Set";
            var tpWagerSet = "Service_TpWagerESports_IM_Settle_Set";
            var logName = "IMDoSettleSportsWork";

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
                        var betDetail = _mainDbClient.Query<SettleTransactions_IMSport>("Service_BettransESport_IM_Bet_Get", parameters: new { _BetId = unsettled.TransId }).FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, betDetail.PlayerId);
                        var transId = betDetail.BetId;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = betDetail.Amount,
                            ValidBet = betDetail.Amount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = betDetail.PlayerId,
                            SettlementType = SettlementType.UnSettled,
                            TicketStatus = TicketStatus.UnSettled,
                            GameID = betDetail.GameId
                        };

                        var settleBetTrans = _mainDbClient.Query<SettleTransactions_IMESport>(settleBetTransGet, parameters: new { unsettled.TransId });

                        if (settleBetTrans.Any())
                        {
                            // 最新一筆的Settle結果
                            var lastBetTransData = settleBetTrans.MaxBy(x => x.TimeStamp);

                            //多次結算結果的Sum為payoutAmount總額
                            var payoutAmount = settleBetTrans.Sum(x => x.Amount);
                            var stake = lastBetTransData.Stake;
                            var winLostAmount = payoutAmount - stake;

                            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                            if (tpCurrency == null)
                            {
                                _logger.LogWarning($"{logName} {lastBetTransData.PlayerId} TpCurrency not found.");
                                continue;
                            }

                            var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<WagerDataIM>("Service_TpWagerESports_IM_Unsettle_Get", new { BetId = transId }, DataBase.Wager).FirstOrDefault();

                            betRecordInfo.WinlostAmount = winLostAmount;
                            betRecordInfo.SettledTime = new DateTimeOffset(lastBetTransData.TimeStamp).ToUnixTimeSeconds();
                            betRecordInfo.TransTime = new DateTimeOffset(tpWagerData.WagerCreationDateTime).ToUnixTimeSeconds();

                            CalculateTicketStatusAndAmount(betRecordInfo, tpWagerData, lastBetTransData.ActionId, out var isRefund);

                            if (tpWagerData.IsSettled == 1)
                            {
                                var settleAmountDataIm = new SettleAmountDataIM
                                {
                                    Stake = betRecordInfo.Stake,
                                    ValidBet = betRecordInfo.ValidBet,
                                    WinLostAmount = betRecordInfo.WinlostAmount
                                };
                                if (!IsBetTransSameAsTpWager(settleAmountDataIm, tpWagerData, tpCurrency, isRefund))
                                {
                                    _logger.LogWarning($"transId: {transId} is not equal " +
                                                     $"settle: {JsonConvert.SerializeObject(settleAmountDataIm)} " +
                                                     $"wager: {JsonConvert.SerializeObject(tpWagerData)}");
                                    continue;
                                }
                            }
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, isSuccess: true);
                                _ = _imBll.SetSettleFlag_Bettrans_IM(settleBetTransSet, transId);
                                _ = _imBll.SetSettleFlag_TpWager_IM(tpWagerSet, transId);
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
                timerESport.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
        {
            var dueTimeSpan = TimeSpan.FromSeconds(30);

            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(1);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromSeconds(30);
            }

            return dueTimeSpan;
        }

        private bool IsBetTransSameAsTpWager(SettleAmountDataIM betTrans, WagerDataIM wager, TpCurrency tpCurrency, bool isRefund)
        {
            if (tpCurrency.ExchangeRate <= 0) return false;

            // 加入幣別匯率轉換，tpWager 是原始資料
            var wagerStake = wager.StakeAmount * tpCurrency.ExchangeRate;
            var wagerWinLostAmount = (isRefund ? wager.BetTradeBuybackAmount - wager.StakeAmount : wager.WinLoss)
                                          * tpCurrency.ExchangeRate;

            //var fWagerWinLostAmount = Math.Round((decimal)wagerWinLostAmount);

            var fWagerStake = Math.Truncate(wagerStake * 10) / 10;
            betTrans.Stake = Math.Truncate(betTrans.Stake * 10) / 10;

            var isSame = fWagerStake == betTrans.Stake
                         && Math.Abs(wagerWinLostAmount - betTrans.WinLostAmount) < Convert.ToDecimal(2);

            if (!isSame)
            {
                _logger.LogWarning(
                    $"wagerStake={fWagerStake}, betTrans.Stake={betTrans.Stake}, wagerWinLostAmount={wagerWinLostAmount}, betTrans.WinLostAmount={betTrans.WinLostAmount}");
            }

            return isSame;
        }

        private void CalculateTicketStatusAndAmount(BetRecordInfo betRecordInfo, WagerDataIM tpWagerData, int actionId, out bool isRefund)
        {
            isRefund = false;

            if (tpWagerData.IsSettled == 1)
            {
                var transType = _imBll.GetTransType(actionId);
                if (transType == TransType.Refunds)
                {
                    betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                    betRecordInfo.TicketStatus = TicketStatus.Refund;
                    betRecordInfo.ValidBet = 0;
                }
                else if (betRecordInfo.GameType == GameType.IM_Sports && tpWagerData.BetTradeStatus.Equals("SOLD", StringComparison.OrdinalIgnoreCase))
                {
                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                    betRecordInfo.ValidBet = 0;
                    isRefund = true;
                }
                else
                {
                    betRecordInfo.SettlementType = SettlementType.SettledForValid;
                }
            }
            else if (tpWagerData.IsCancelled == 1)
            {
                betRecordInfo.SettlementType = SettlementType.SettledForValid;
                betRecordInfo.ValidBet = 0;
            }
        }
    }
}
