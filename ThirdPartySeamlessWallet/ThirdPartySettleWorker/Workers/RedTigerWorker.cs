using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class RedTigerWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.RedTiger;
        private Timer _timerSettleRedTiger;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<RedTigerWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public RedTigerWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<RedTigerWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleRedTiger = new Timer(DoSettleRedTigerWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleRedTiger?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRedTigerWork(object state)
        {
            var forContinue = false;
            var thisTp = (int)Tp.RedTiger;
            var gameType = GameType.RedTiger_RNG;
            var category = TpCategory.Slots;
            var logName = "Settle_RedTiger_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);

                if (tpUnsettleList != null)
                {
                    forContinue = tpUnsettleList.Count() == 200;
                    foreach (var tpUnsettle in tpUnsettleList)
                    {
                        try
                        {
                            var unsettled = _settledBLL.GetUnsettledList_RedTiger(tpUnsettle.TransId);
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId),
                                GameID = unsettled.GameId,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, false);
                            });
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogError(ex1, $"RedTiger Settle exception. {ex1.Message} || TransId : {tpUnsettle.TransId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                _timerSettleRedTiger.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
