using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class DGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.DG;
        private Timer _timerLC;
        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<DGWorker> _logger;
        private readonly string _tpName;

        public DGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<DGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleDGWork, GameType.DG_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private void DoSettleDGWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var tpId = (int)Tp.DG;
            var logName = "DGDoSettleLiveCasinoWork";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_DG>("Service_BettransLC_DG_Get_V2",
                            new { tpUnsettle.TransId }, DataBase.Main);
                        var wager = _settledBLL.GetSettle_TpWagerDG(tpUnsettle.TransId);
                        if (wager != null)
                        {
                            //会员发红包、会员抢红包、主播小费、公司发红包、博饼记录、荷官小费
                            if (wager.GameType == 2)
                            {
                                Task.Run(() =>
                                {
                                    _settledBLL.SetSettleFlag_TpWagerDG(tpUnsettle.TransId);
                                    _settledBLL.SetSettleFlag_DG(tpUnsettle.TransId);
                                });
                                continue;
                            }

                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, wager.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

                            if (tpCurrency.ExchangeRate > 1)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.ValidBet *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = tpId,
                                Stake = wager.Stake,
                                ValidBet = wager.ValidBet,
                                WinlostAmount = wager.WinLostAmount,
                                TransId = tpUnsettle.TransId,
                                GameType = GameType.DG_LiveCasino,
                                TpUserName = wager.Account,
                                SettledTime = wager.SettleTime,
                                TransTime = wager.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = wager.GameId.ToString(),
                            };
                            
                            var winLossAmountComparison = DG_WinLossAmountComparison(wager.WinLostAmount, settleData);
                            if (!winLossAmountComparison.Comparison)
                            {
                                _logger.LogWarning($"DG WinLossAmount Comparison Incompatible TransId : {betRecordInfo.TransId} - WagerWinLossAmount {wager.WinLostAmount:F2}, betTrans : {winLossAmountComparison.WinLossAmount}");
                                continue;
                            }

                            _ = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            Task.Run(() =>
                            {
                                _settledBLL.SetSettleFlag_TpWagerDG(tpUnsettle.TransId);
                                _settledBLL.SetSettleFlag_DG(tpUnsettle.TransId);
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
                    }

                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogWarning(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        #region private
        
        private class WinLossAmountModel
        {
            public bool Comparison { get; set; }
            public decimal WinLossAmount { get; set; }
        }
        private WinLossAmountModel DG_WinLossAmountComparison(decimal wagerWinLossAmount, IEnumerable<SettleData_DG> settleData)
        {
            var settleDataDgs = settleData.ToList();
            var betAmount = settleDataDgs.Where(t => t.TicketType == TransferType.PlaceBet).Select(t => t.Amount);
            var winLossAmount = settleDataDgs.Where(t => t.TicketType == TransferType.Payout).Select(t => t.Amount);
            
            var totalBetAmount = betAmount.Sum();
            var totalWinLossAmount = winLossAmount.Sum();
            var totalWinLossAmountComparison = totalWinLossAmount - totalBetAmount;

            var result = new WinLossAmountModel()
            {
                Comparison = wagerWinLossAmount == totalWinLossAmountComparison,
                WinLossAmount = totalWinLossAmountComparison
            };
            
            return result;
        }
        
        #endregion
    }
}