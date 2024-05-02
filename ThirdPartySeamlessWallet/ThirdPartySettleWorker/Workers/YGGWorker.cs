using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class YGGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.YGG; }

        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<YGGWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly string _tpName;


        public YGGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<YGGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoSettle, GameType.YGG_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = TpCategory.Slots;

            var logName = $"SettleYGG";
            var getSP = $"Service_BettransRng_YGG_Unsettle_Get_V3";
            var setSP = $"Service_BettransRng_YGG_Settle_Set_V3";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { TransId = transId }, DataBase.Main).First();
                        var wager = _settledBLL.GetSettle_TpWager_YGG(settleData.TransId);

                        if (wager == null) continue; // wager 沒注單，無法比對(投注額、輸贏值)，故不結算

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, settleData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        if (!CanSettle(wager, settleData, tpCurrency)) continue;

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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private bool CanSettle(SettleData_YGG wager, SettleData settleData, TpCurrency tpCurrency)
        {
            if (tpCurrency.ExchangeRate <= 0) return false;

            // 加入幣別匯率轉換，tpWager 是原始資料
            decimal wagerStake = wager.Stake * tpCurrency.ExchangeRate;
            decimal? wagerWinLostAmount = wager.WinLostAmount * tpCurrency.ExchangeRate;

            return wagerStake == settleData.Stake && wagerWinLostAmount == settleData.WinLostAmount;
        }
    }
}
