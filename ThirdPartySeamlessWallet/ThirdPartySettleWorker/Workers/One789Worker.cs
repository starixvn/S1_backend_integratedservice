using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class One789Worker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.One789; }

        private Timer timerSettleOne789LT;
        private Timer timerSettleOne789LT_789;
        private Timer timerSettleOne789LC;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<One789Worker> _logger;
        private readonly string _tpName;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public One789Worker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<One789Worker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleOne789LT = new Timer(DoSettle, GameType.One789_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT_789 = new Timer(DoSettle, GameType.One789_Lottery_789, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LC = new Timer(DoSettle, GameType.One789_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleOne789LT?.Change(Timeout.Infinite, 0);
            timerSettleOne789LT_789?.Change(Timeout.Infinite, 0);
            timerSettleOne789LC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            var gameType = (GameType)state;
            var logName = gameType switch
            {
                GameType.One789_Lottery => "Settle_One789_LT",
                GameType.One789_Lottery_789 => "Settle_One789_LT_789",
                GameType.One789_LiveCasino => "Settle_One789_LC",
                _ => throw new NotImplementedException(),
            };
            var tpCategory = gameType switch
            {
                GameType.One789_Lottery => TpCategory.Lottery,
                GameType.One789_Lottery_789 => TpCategory.Lottery,
                GameType.One789_LiveCasino => TpCategory.LiveCasino,
                _ => throw new NotImplementedException(),
            };

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var wager = _settledBLL.GetSettle_TpWagerOne789((int)gameType, tpUnsettle.TransId);
                        var unsettled = _settledBLL.GetUnsettledList_One789((int)gameType, tpUnsettle.TransId);

                        if (wager != null)
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, wager.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                            if (tpCurrency.ExchangeRate > 1)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.ValidBet *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            bool isRefund = false;
                            if (unsettled.TicketType == (int)BettransType.Bet && unsettled.TicketStatus == (int)TicketStatus.Refund && wager.Status == 0)
                            {
                                isRefund = true;
                            }

                            if ((wager.Stake == unsettled.Stake && wager.WinLostAmount == unsettled.WinLostAmount) || isRefund)
                            {
                                var betRecordInfo = new BetRecordInfo
                                {
                                    TpId = (int)Tp,
                                    Stake = wager.Stake,
                                    ValidBet = wager.ValidBet,
                                    WinlostAmount = wager.WinLostAmount,
                                    TransId = wager.TransId,
                                    GameType = gameType,
                                    TpUserName = wager.Account,
                                    SettledTime = wager.SettleTime,
                                    TransTime = wager.TransTime,
                                    SettlementType = SettlementType.SettledForValid,
                                    GameID = wager.GameId,
                                };

                                if (isRefund)
                                {
                                    betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                    betRecordInfo.TicketStatus = TicketStatus.Refund;
                                }

                                var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);

                                if (result == 1)
                                {
                                    Task.Run(() =>
                                    {
                                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                                    });
                                }
                            }
                        }
                        else
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                            //彩票進行未結算流程
                            if (tpCategory == TpCategory.Lottery)
                            {
                                /*
                                 * 針對已寫入TpBetRecord SettlementType.UnSettled但後續Cancel
                                 * 因部分情況不一定會有wager，需使用Bettrans判斷來更新TpBetRecord狀態
                                 */
                                bool isRefund = false;
                                if (unsettled.TicketType == (int)BettransType.Bet && unsettled.TicketStatus == (int)TicketStatus.Refund)
                                {
                                    isRefund = true;
                                }

                                var betRecordInfo = new BetRecordInfo
                                {
                                    TpId = (int)Tp,
                                    Stake = unsettled.Stake,
                                    ValidBet = unsettled.Stake,
                                    WinlostAmount = 0,
                                    TransId = unsettled.TransId,
                                    GameType = gameType,
                                    TpUserName = unsettled.Account,
                                    SettledTime = unsettled.SettleTime,
                                    TransTime = unsettled.TransTime,
                                    TicketStatus = TicketStatus.UnSettled,
                                    SettlementType = SettlementType.UnSettled
                                };

                                if (isRefund)
                                {
                                    betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                    betRecordInfo.TicketStatus = TicketStatus.Refund;
                                }
                                var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);

                                if (result == 1)
                                {
                                    Task.Run(() =>
                                    {
                                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"One789 Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{logName} || {ex.Message}");
            }
            finally
            {
                if (gameType == GameType.One789_LiveCasino)
                {
                    timerSettleOne789LC.Change(dueTimeSpan, TimeSpan.Zero);
                }

                if (gameType == GameType.One789_Lottery)
                {
                    timerSettleOne789LT.Change(dueTimeSpan, TimeSpan.Zero);
                }

                if (gameType == GameType.One789_Lottery_789)
                {
                    timerSettleOne789LT_789.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
    }
}
