using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    internal class KMWoker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.KM;

        private Timer timerSettleKMPoker;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<KMWoker> _logger;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;

        public KMWoker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<KMWoker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleKMPoker = new Timer(DoSettleKMPokerWork, GameType.KM_Poker, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettleKMPoker?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// TpWagerKM 投注資料做結算
        /// </summary>
        /// <param name="state"></param>
        private void DoSettleKMPokerWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.KM;
            string logName = "Settle_KM_Poker";
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            const string getWagerSp = "Service_TpWagerKM_Get"; // 取得下注紀錄

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, GameType.KM_Poker);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {GameType.KM_Poker} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getWagerSp, new { ugsbetid = tpUnsettle.TransId }, DataBase.Wager).ToList().FirstOrDefault();

                        if(settleData != null)
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, settleData.Account);
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = settleData.Stake,
                                ValidBet = settleData.ValidBet,
                                WinlostAmount = settleData.WinLostAmount,
                                TransId = settleData.TransId,
                                Revenue = settleData.Revenue,
                                GameType = GameType.KM_Poker,
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
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.KM_Poker, tpUnsettle.TransId, true);
                                });
                            }
                        }
                        else
                        {
                            //為了解決有些有拉到WAGER資料 但我們根本沒有收到注單
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.KM_Poker, tpUnsettle.TransId, true);
                            });
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, GameType.KM_Poker, tpUnsettle.TransId, false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"KM Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
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
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerSettleKMPoker.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
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

            return dueTimeSpan;
        }
    }
}
