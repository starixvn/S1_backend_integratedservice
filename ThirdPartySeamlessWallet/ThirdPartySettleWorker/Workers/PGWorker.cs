using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.PG; }

        private Timer novaTimer;
        private Timer goalTimer;
        private Timer bwingTimer;
        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<PGWorker> _logger;
        private readonly string _tpName;


        public PGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<PGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            novaTimer = new Timer(DoSettle, 1, TimeSpan.Zero, TimeSpan.Zero);
            goalTimer = new Timer(DoSettle, 2, TimeSpan.Zero, TimeSpan.Zero);
            bwingTimer = new Timer(DoSettle, 3, TimeSpan.Zero, TimeSpan.Zero);
            timer = new Timer(DoSettle, 0, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            novaTimer?.Change(Timeout.Infinite, 0);
            goalTimer?.Change(Timeout.Infinite, 0);
            bwingTimer?.Change(Timeout.Infinite, 0);
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var siteId =(int)state;
            var forContinue = false;
            var isError = false;
            var gametype = GameType.PG_RNG;
            var tpCategory = TpCategory.Slots;

            var logName = $"SettlePG";
            var getSP = $"Service_BettransRng_PG_Unsettle_Get_V2";
            var getSP_Fix = $"Service_BettransRng_PG_Unsettle_Fix_Get";
            var setSP = $"Service_BettransRng_PG_Settle_Set_V2";
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype, 5000);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() >= 200;
                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { TransId = transId }, DataBase.Main).First();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, settleData.Account);
                        // 依用戶資訊判斷siteId來決定是否跳過
                        if (siteId != 0 && tpCustInfo.SiteId != siteId)
                        {
                            continue;
                        }

                        if (siteId == 0  && tpCustInfo.SiteId is 1 or 2 or 3)
                        {
                            continue;
                        }

                        var fix = false;
                        if ((settleData.TransTime >= 1712320200 && settleData.TransTime <= 1712348100) && (tpCustInfo.CurrencyId == 51 || tpCustInfo.CurrencyId == 15))
                        {
                            fix = true;
                            settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP_Fix, new { TransId = transId }, DataBase.Main).First();
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = settleData.Stake,
                            ValidBet = settleData.Stake,
                            WinlostAmount = settleData.WinLostAmount,
                            TransId = settleData.TransId,
                            GameType = gametype,
                            TpUserName = settleData.Account,
                            SettledTime = settleData.SettleTime,
                            TransTime = settleData.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            TicketStatus = settleData.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            GameID = settleData.GameId,
                        };

                        // fix issue ticket to refund and not roll
                        if (fix && settleData.TicketStatus > 0)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gametype, transId);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gametype, transId, false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {transId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{logName} exMessage:{ex.Message}");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(10);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (siteId == 1) 
                {
                    novaTimer.Change(dueTimeSpan, TimeSpan.Zero);   
                }
                else if (siteId == 2)
                {
                    goalTimer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else if (siteId == 3)
                {
                    bwingTimer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
    }
}
