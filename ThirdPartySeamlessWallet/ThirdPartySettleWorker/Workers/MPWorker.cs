using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class MPWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.MP;
        private Timer _timerSettleMP;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<MPWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public MPWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<MPWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleMP = new Timer(DoSettleMPWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleMP?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleMPWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.MP;
            var logName = "Settle_MP_PK";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListPK_MP();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                                unsettled.Revenue *= tpCurrency.ExchangeRate;
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                Revenue = unsettled.Revenue,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = (GameType)unsettled.GameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };
                            var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            _settledBLL.SetSettleFlag_TpWagerPK_MP(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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

                _timerSettleMP?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
