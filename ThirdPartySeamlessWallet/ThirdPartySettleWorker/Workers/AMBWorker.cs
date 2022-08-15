using Serilog.Context;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class AMBWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.AMB;

        private Timer _timerRng;
        private Timer _timerPoker;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<AMBWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;

        public AMBWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<AMBWorker>>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerRng = new Timer(DoSettleRng, null, TimeSpan.Zero, TimeSpan.Zero);
            _timerPoker = new Timer(DoSettlePoker, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerRng?.Change(Timeout.Infinite, 0);
            _timerPoker?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRng(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = GameType.AMB_RNG;
            var tpCategory = TpCategory.Slots;
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                var unsettleList = tpUnsettleList.ToList();
                if (!unsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = unsettleList.Count() == 200;
                foreach (var tpUnsettle in unsettleList)
                {
                    try
                    {
                        var unsettleData = _partitionService.GetUnsettleDataList<SettleData>("Service_BettransRng_AMB_Unsettle_Get", tpUnsettle).FirstOrDefault();
                        if (unsettleData != null)
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettleData.Account);
                            if (tpCustInfo == null)
                            {
                                throw new UnsettleNotFoundException($"{unsettleData.Account} TpCustInfo not found.");
                            }

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = unsettleData.Stake,
                                ValidBet = unsettleData.ValidBet,
                                WinlostAmount = unsettleData.WinLostAmount,
                                TransId = tpUnsettle.TransId,
                                GameType = gametype,
                                TpUserName = unsettleData.Account,
                                SettledTime = unsettleData.SettleTime,
                                TransTime = unsettleData.TransTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                                TicketStatus = SetTicketStatus(unsettleData.WinLostAmount)
                            };

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, true);
                                });
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"TransId:{tpUnsettle.TransId} settle failed, exMessage:{ex1.Message}");
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

                _timerRng?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private TicketStatus SetTicketStatus(decimal winLostAmount)
        {
            if (winLostAmount == 0) return TicketStatus.Draw;

            if (winLostAmount > 0) return TicketStatus.Win;
            
            return TicketStatus.Lost;
        }

        private void DoSettlePoker(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.AMB;
            var gameType = GameType.AMB_Poker;
            var logName = "Settle_AMB";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListPK_AMB();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;
                            decimal revenue = (decimal)unsettled.Revenue;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                Revenue = revenue,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            if (unsettled.IsRefund == 1)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Refund;
                            }

                            var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            _settledBLL.SetSettleFlag_BettransPK_AMB(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
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

                _timerPoker?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
