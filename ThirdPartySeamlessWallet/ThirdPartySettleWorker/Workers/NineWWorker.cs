using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class NineWWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.NineW;
        private Timer _timerSports;
        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<NineWWorker> _logger;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public NineWWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<NineWWorker>>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSports = new Timer(DoSettle, GameType.BTI_Sports, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSports?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            var gameType = (GameType)state;
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            try
            {
                var unsettledList = _tpUnSettledBLL.GetTpUnsettledList<SettleData>("Service_TpWager9W_Unsettle_Get", DataBase.Wager);

                if (unsettledList != null)
                {
                    if (unsettledList.Count() == 200)
                    {
                        dueTimeSpan = TimeSpan.FromSeconds(1);
                    }

                    var spName = "Service_TpWager9W_Settle_Set";
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                TicketStatus = unsettled.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Sports);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetSettleFlag(spName, unsettled.TransId, dataBase: DataBase.Wager);
                                });
                            }

                        }
                        catch (Exception ex)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(spName, unsettled.TransId, isSuccess: false, DataBase.Wager);
                            });
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogError(ex, $"{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"NineW Settle exception.");
            }
            finally
            {
                _timerSports.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
