using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class EVOWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.EVO;
        private Timer _timerLC;
        private Timer _timerReward;
        private readonly SettledBLL _settledBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly ILogger<EVOWorker> _logger;
        private readonly string _tpName;

        public EVOWorker(IServiceProvider provider)
        {
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<EVOWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleLC, GameType.EVO_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            _timerReward = new Timer(DoSettleReward,
                new Dictionary<Tp, GameType>
                {
                    { Tp.EVO, GameType.EVO_LiveCasino },
                    { Tp.RedTiger, GameType.RedTiger_RNG },
                    { Tp.Netent, GameType.Netent_RNG },
                    { Tp.NLC, GameType.NLC_RNG }
                },
                TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLC?.Change(Timeout.Infinite, 0);
            _timerReward?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleLC(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var logName = "EVODoSettleLiveCasinoWork";
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
                        var transId = unsettled.TransId;
                        var unsettledData = _settledBll.GetUnsettled_EVO(transId);
                        var wager = _settledBll.GetSettle_TpWagerLC_EVO(transId);

                        if (wager != null)
                        {
                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp, unsettledData.Account);
                            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                wager.Stake *= tpCurrency.ExchangeRate;
                                wager.WinLostAmount *= tpCurrency.ExchangeRate;
                            }
                            var stake = unsettledData.Stake;
                            var validBet = unsettledData.Stake;
                            var winLostAmount = unsettledData.WinLostAmount;

                            var dWagerStake = Math.Truncate(wager.Stake * 10) / 10;
                            var dBettransStake = Math.Truncate(stake * 10) / 10;
                            var dDiffStake = Math.Abs(dWagerStake - dBettransStake);

                            if (dDiffStake > 0.5m)
                            {
                                LogContext.PushProperty("TP", Tp.EVO);
                                _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettledData)}");
                                continue;
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettledData.TransId,
                                GameType = gameType,
                                TpUserName = unsettledData.Account,
                                SettledTime = wager.SettleTime,
                                TransTime = wager.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = wager.GameId,
                            };

                            if (betRecordInfo.WinlostAmount == 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Draw;
                                betRecordInfo.ValidBet = 0m;
                            }

                            if (string.Equals(wager.status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                betRecordInfo.TicketStatus = TicketStatus.Refund;
                            }

                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettledData.TransId, isSuccess: true);
                                    _settledBll.SetSettleFlag_EVO(unsettledData.TransId);
                                });
                            }
                        }
                        /*
                        else if (wager == null && unsettled.PartitionTime < DateTime.UtcNow.AddHours(-1) && unsettledData.GameId == "crazycoinflip")
                        {
                            // 因Crazy Coin Flip對方異常API取不到資料 直接取bettrans結算
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = unsettledData.Stake,
                                ValidBet = unsettledData.Stake,
                                WinlostAmount = unsettledData.WinLostAmount,
                                TransId = unsettledData.TransId,
                                GameType = gameType,
                                TpUserName = unsettledData.Account,
                                SettledTime = unsettledData.SettleTime,
                                TransTime = unsettledData.TransTime,
                                SettlementType = SettlementType.SettledForValid,
                                GameID = unsettledData.GameId,
                            };

                            if (betRecordInfo.WinlostAmount == 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Draw;
                            }

                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp, unsettledData.Account);
                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettledData.TransId, isSuccess: true);
                                    _settledBll.SetSettleFlag_EVO(unsettledData.TransId);
                                });
                            }
                        }
                        */
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBll.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
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

        private void DoSettleReward(object state)
        {
            var forContinue = false;
            var isError = false;
            var tpList = (Dictionary<Tp, GameType>)state;
            var logName = "EVODoSettleRewardWork";
            try
            {
                foreach (var (tp, gameType) in tpList)
                {
                    var tpName = Enum.GetName(typeof(Tp), tp);
                    LogContext.PushProperty("TP", tpName);
                    LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

                    var tpUnsettleList = _tpUnSettledBll.GetTpUnsettleList(tp, gameType);
                    if (!tpUnsettleList.Any())
                    {
                        _logger.LogInformation($"{tpName} {gameType} no unsettle data.");
                        continue;
                    }

                    var forContinueDetail = tpUnsettleList.Count() == 200;
                    foreach (var unsettled in tpUnsettleList)
                    {
                        try
                        {
                            var rewardData = _settledBll.GetUnsettledReward_EVO(unsettled.TransId);
                            if (rewardData == null)
                            {
                                continue;
                            }

                            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache(rewardData.TpId, rewardData.userId);

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = rewardData.TpId,
                                GameType = (GameType)rewardData.TpGameType,
                                TpUserName = rewardData.userId,
                                Stake = 0,
                                ValidBet = 0,
                                WinlostAmount = rewardData.PromoTransactionAmount,
                                TransId = rewardData.PromoTransactionId,
                                SettledTime = rewardData.CreateTime,
                                TransTime = rewardData.CreateTime,
                                SettlementType = SettlementType.SettledForValid,
                            };

                            var tpCategory = rewardData.TpId == (int)Tp.EVO ? TpCategory.LiveCasino : TpCategory.Slots;

                            var result = _settledBll.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBll.SetTpUnsettle(tp, gameType, unsettled.TransId, isSuccess: true);
                                    _settledBll.SetRewardSettleFlag_EVO(rewardData.SQ);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            Task.Run(() =>
                            {
                                _tpUnSettledBll.SetTpUnsettle(tp, gameType, unsettled.TransId, isSuccess: false);
                            });
                            isError = true;
                            _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                        }
                    }

                    //check tpList forContinue
                    if (forContinueDetail)
                    {
                        forContinue = forContinueDetail;
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
                _timerReward.Change(dueTimeSpan, TimeSpan.Zero);
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
