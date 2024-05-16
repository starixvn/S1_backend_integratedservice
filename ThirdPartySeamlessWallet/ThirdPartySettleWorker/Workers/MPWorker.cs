using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class MPWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.MP;
        private Timer _timerSettleMP;
        private Timer _timerSettleMPNoRevenue;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<MPWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly string _tpName;

        public MPWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<MPWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleMP = new Timer(DoSettleMPWork, GameType.MP_Poker, TimeSpan.Zero, TimeSpan.Zero);
            _timerSettleMPNoRevenue = new Timer(DoSettleMPWork, GameType.MP_Poker_None, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleMP?.Change(Timeout.Infinite, 0);
            _timerSettleMPNoRevenue?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleMPWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.MP;
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            var gameType = (GameType)state;
            var logName = gameType switch
            {
                GameType.MP_Poker => "Settle_MP_PK",
                GameType.MP_Poker_None => "Settle_MPNoRevenue_PK",
                _ => throw new NotImplementedException(),
            };
            const string getWagerSp = "Service_TpWagerPK_MP_Get"; // 取得下注紀錄

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getWagerSp, new { GameID = tpUnsettle.TransId }, DataBase.Wager).ToList().FirstOrDefault();

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, settleData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            settleData.Stake *= tpCurrency.ExchangeRate;
                            settleData.ValidBet *= tpCurrency.ExchangeRate;
                            settleData.WinLostAmount *= tpCurrency.ExchangeRate;
                            settleData.Revenue *= tpCurrency.ExchangeRate;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = settleData.Stake,
                            ValidBet = settleData.ValidBet,
                            Revenue = settleData.Revenue,
                            WinlostAmount = settleData.WinLostAmount,
                            TransId = settleData.TransId,
                            GameType = gameType,
                            TpUserName = settleData.Account,
                            SettledTime = settleData.SettleTime,
                            TransTime = settleData.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = settleData.GameId,
                        };
                        var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);

                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"{logName} Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
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
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _timerSettleMP?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
