using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class EBetWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.EBet;
        private Timer _timerLC;
        private readonly SettledBLL _settledBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly ILogger<EBetWorker> _logger;
        private readonly string _tpName;

        public EBetWorker(IServiceProvider provider)
        {
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<EBetWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleLC, GameType.EBet_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleLC(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var logName = "EBetDoSettleLiveCasinoWork";
            try
            {
                var tpUnsettleList = _tpUnSettledBll.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var unsettledTrans = _settledBll.GetUnsettledListLC_EBet_ByTrans(unsettled.TransId);

                        if (unsettledTrans != null)
                        {
                            try
                            {
                                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp, unsettledTrans.Account);

                                var stake = unsettledTrans.Stake;
                                var validBet = unsettledTrans.ValidBet;
                                var winLostAmount = unsettledTrans.WinLostAmount;

                                var betRecordInfo = new BetRecordInfo
                                {
                                    TpId = (int)Tp,
                                    Stake = stake,
                                    ValidBet = validBet,
                                    WinlostAmount = winLostAmount,
                                    TransId = unsettled.TransId,
                                    GameType = GameType.EBet_LiveCasino,
                                    TpUserName = unsettledTrans.Account,
                                    SettledTime = unsettledTrans.SettleTime / 1000,
                                    TransTime = unsettledTrans.TransTime / 1000,
                                    SettlementType = SettlementType.SettledForValid,
                                    GameID = unsettledTrans.GameId,
                                };
                                // check refund
                                if (unsettledTrans.TicketStatus > 0)
                                {
                                    betRecordInfo.TicketStatus = TicketStatus.Refund;
                                    betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                }
                                _settledBll.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                Task.Run(() =>
                                {
                                    _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                                    _settledBll.SetSettleFlag_EBet(unsettled.TransId);
                                });
                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", _tpName);
                                isError = true;
                                _logger.LogError(ex1,$"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
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
                isError = true;
                _logger.LogError(ex, $"{logName}:{ex.Message}");
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                _timerLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(1);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromMinutes(1);
            }

            return dueTimeSpan;
        }

    }
}
