using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class SexyWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Sexy;
        private Timer _timerLC;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ILogger<SexyWorker> _logger;
        private readonly string _tpName;

        public SexyWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<SexyWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleSexyWork, GameType.Sexy_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoSettleSexyWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var tpId = (int)Tp.Sexy;
            var category = TpCategory.LiveCasino;
            var logName = "SexyDoSettleLiveCasinoWork";

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
                        // SexyLC沒有TpWager，都是從BettransLC取得資料並且同一張表更新。
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_Sexy>("Service_BettransLC_AWC_Sexy_Unsettle_Get_V2",
                            new { TransId = tpUnsettle.TransId }, DataBase.Main).FirstOrDefault();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, settleData.Account);

                        var stake = settleData.Stake;
                        var validBet = settleData.ValidBet;
                        var winLostAmount = settleData.WinLostAmount;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = tpId,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = settleData.TransId,
                            GameType = gameType,
                            TpUserName = settleData.Account,
                            SettledTime = settleData.SettleTime,
                            TransTime = settleData.TransTime,
                            SettlementType = settleData.TicketStatus == 0 ? SettlementType.UnSettled : SettlementType.SettledForValid,
                            TicketStatus = (TicketStatus)settleData.TicketStatus,
                            GameID = settleData.GameId
                        };

                        // check refund
                        if (betRecordInfo.TicketStatus == TicketStatus.Refund)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        }

                        _ = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                        Task.Run(() =>
                        {
                            _settledBLL.SetSettleFlag_BettransLC_AWC_Sexy(settleData.TransId);
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                        });

                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex, $"{logName}:{ex.Message}");
                    }
                }

            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
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

    }
}