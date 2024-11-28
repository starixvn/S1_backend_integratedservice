using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class YLWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.YL;
        private Timer _timerSettleYLFish;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<YLWorker> _logger;

        public YLWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<YLWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleYLFish = new Timer(DoSettleYLFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleYLFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleYLFishWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.YL;
            var gameType = GameType.YL_Fish;
            var category = TpCategory.Fishing;
            var logName = "Settle_YL_Fish";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListFish_YL();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = unsettled.TicketStatus == (int)TicketStatus.Refund ? SettlementType.SettledForInvalid : SettlementType.SettledForValid,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus,
                                GameID = unsettled.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                            _settledBLL.SetSettleFlagFish_YL(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName, ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName, ex);
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

                _timerSettleYLFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
