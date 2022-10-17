using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class V8Worker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.V8;
        private Timer timerSettleV8;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<V8Worker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public V8Worker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<V8Worker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleV8 = new Timer(DoSettleV8Work, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleV8?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleV8Work(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.V8;
            var logName = "Settle_V8_PK";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListPK_V8();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var account = unsettled.Account;
                            if (account.IndexOf("_") > -1)
                            {
                                account = account[(account.IndexOf("_") + 1)..];
                            }

                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                                unsettled.Revenue *= tpCurrency.ExchangeRate;
                            }

                            // revenue games
                            GameType gameType;
                            int[] kindId = { 220, 600, 610, 620, 630, 680, 830, 860, 890, 930, 3101, 3890, 8100, 8540, 8600, 8700 };
                            if (kindId.Contains(unsettled.KindID))
                            {
                                gameType = GameType.V8_Poker;
                            }
                            else
                            {
                                gameType = GameType.V8_Poker_None;
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                Revenue = unsettled.Revenue,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.GameID,
                                GameType = gameType,
                                TpUserName = account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };
                            var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            _settledBLL.SetSettleFlag_TpWagerPK_V8(unsettled.GameID);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
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

                timerSettleV8.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
