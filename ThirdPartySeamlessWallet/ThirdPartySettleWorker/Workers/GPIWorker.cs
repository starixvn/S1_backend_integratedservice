using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class GPIWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.GPI;
        private Timer _timerSettleGpi;
        private Timer _timerLT_Keno;
        private Timer _timerLT_Sode;
        private Timer _timerLT_Thai;
        private Timer _timerLT_Fast3;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<GPIWorker> _logger;
        private readonly string _tpName;

        public GPIWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<GPIWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettleGpi = new Timer(DoSettleGPIWork, GameType.GPI_RNG, TimeSpan.Zero, TimeSpan.Zero);
            _timerLT_Keno = new Timer(DoSettleGPILotteryWork, GameType.GPI_Keno, TimeSpan.Zero, TimeSpan.Zero);
            _timerLT_Sode = new Timer(DoSettleGPILotteryWork, GameType.GPI_Sode, TimeSpan.Zero, TimeSpan.Zero);
            _timerLT_Thai = new Timer(DoSettleGPILotteryWork, GameType.GPI_Thai_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            _timerLT_Fast3 = new Timer(DoSettleGPILotteryWork, GameType.GPI_Fast3, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettleGpi?.Change(Timeout.Infinite, 0);
            _timerLT_Keno?.Change(Timeout.Infinite, 0);
            _timerLT_Sode?.Change(Timeout.Infinite, 0);
            _timerLT_Thai?.Change(Timeout.Infinite, 0);
            _timerLT_Fast3?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleGPIWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.GPI;
            var gameType = (GameType)state;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                
                if (tpUnsettleList != null)
                {
                    forContinue = tpUnsettleList.Count() == 200;
                    foreach (var unsettled in tpUnsettleList)
                    {
                        try
                        {
                            var settleDetail =  _settledBLL.GetUnsettledListRNG_GPI(unsettled.TransId).First();
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, settleDetail.cust_id);

                            var stake = settleDetail.Stake;
                            var validBet = settleDetail.Stake;
                            var winLostAmount = settleDetail.WinLostAmount;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = settleDetail.round_id,
                                GameType = GameType.GPI_RNG,
                                TpUserName = settleDetail.cust_id,
                                SettledTime = settleDetail.SettleTime,
                                TransTime = settleDetail.TransTime,
                                SettlementType = _settledBLL.GetSettlementType(gameType, settleDetail.game_id),
                                GameID = settleDetail.game_id,
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, settleDetail.round_id, isSuccess: true);
                                _settledBLL.SetSettleFlagRNG_GPI(settleDetail.round_id, settleDetail.cust_id, 1);
                            });
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError(ex1, $"SettleGPI:{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex, ex.Message);
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

                _timerSettleGpi.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleGPILotteryWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = TpCategory.Lottery;
            var bettransGet = "Service_BettransLT_GPI_Settle_Get";
            var wagerGet = "Service_TpWagerLT_GPI_Settle_Get";
            var wagerSet = "Service_TpWagerLT_GPI_Settle_Set";
            string logName = $"Settle_GPI_Lottery_{(int)gametype}";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gametype} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        // 結帳情形
                        bool settled = false;
                        // 取消情形
                        bool cancel = false;

                        var bettrans = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(bettransGet, new { unsettled.TransId }, DataBase.Main).FirstOrDefault();
                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleDataRNG_GPI>(wagerGet, new { round_id = unsettled.TransId }, DataBase.Wager).FirstOrDefault();

                        // 判斷是否bettrans是否有收過rollback
                        if (bettrans.TicketStatus == 1 && wager == null)
                        {
                            // 收到rollback但wager無資料 更新TpUnsettle為完成
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:rollback no wager\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, isSuccess: true);
                            continue;
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, bettrans.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        var settledTime = bettrans.SettleTime;
                        var transTime = bettrans.TransTime;

                        if (wager != null)
                        {
                            settled = true;
                            settledTime = wager.SettleTime;

                            if (wager.status == 3)
                            {
                                // Open bets
                                settled = false;
                            }
                            if (wager.status == 2)
                            {
                                // or Void transactions
                                cancel = true;
                            }

                            if (tpCurrency.ExchangeRate != 0)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.ValidBet *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            if (wager.Stake != bettrans.Stake || wager.WinLostAmount != bettrans.WinLostAmount)
                            {
                                LogContext.PushProperty("TP", _tpName);
                                _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                                continue;
                            }
                        }

                        decimal stake = bettrans.Stake;
                        decimal validBet = bettrans.ValidBet;
                        decimal winLostAmount = bettrans.WinLostAmount;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = (settled) ? winLostAmount : 0,
                            TransId = bettrans.TransId,
                            GameType = gametype,
                            TpUserName = bettrans.Account,
                            SettledTime = settledTime,
                            TransTime = transTime,
                            SettlementType = (settled) ? SettlementType.SettledForValid : SettlementType.UnSettled,
                            GameID = wager?.game_id ?? string.Empty,
                        };

                        if (settled && cancel)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _ = _tpUnSettledBLL.SetSettleFlag(wagerSet, Tp, gametype, unsettled.TransId, true, DataBase.Wager);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message} || TransId : {unsettled.TransId}");
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
                _logger.LogError(ex, ex.Message);
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

                switch (gametype)
                {
                    case GameType.GPI_Keno:
                        _timerLT_Keno?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.GPI_Sode:
                        _timerLT_Sode?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.GPI_Thai_Lottery:
                        _timerLT_Thai?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.GPI_Fast3:
                        _timerLT_Fast3?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
