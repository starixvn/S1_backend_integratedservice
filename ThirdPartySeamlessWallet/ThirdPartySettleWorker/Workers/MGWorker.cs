using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class MGWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.MG; }

        private Timer timerRNG;
        private Timer timerLC;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<MGWorker> _logger;
        private readonly string _tpName;


        public MGWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<MGWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(DoSettleRng, null, TimeSpan.Zero, TimeSpan.Zero);
            timerLC = new Timer(DoSettleLc, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRng(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = GameType.MG_RNG;
            var tpCategory = TpCategory.Slots;

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_MG>("Service_BettransRng_MG_Unsettle_Get_V4",
                            new { tpUnsettle.TransId }, DataBase.Main).First();

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, settleData.Account);

                        long settleTime = convertTpBetRecordTime(settleData.SettleTime, settleData.SettleTimeNew);
                        long transTime = convertTpBetRecordTime(settleData.TransTime, settleData.TransTimeNew);

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = settleData.Stake,
                            ValidBet = settleData.ValidBet,
                            WinlostAmount = settleData.WinLostAmount,
                            TransId = settleData.TransId,
                            GameType = gametype,
                            TpUserName = settleData.Account,
                            SettledTime = settleTime,
                            TransTime = transTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, settleData.GameId),
                            TicketStatus = settleData.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            GameID = settleData.GameId,
                        };

                        if (betRecordInfo.WinlostAmount == 0)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Draw;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag("Service_BettransRng_MG_Settle_Set_V2", tpUnsettle.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetSettleFlag("Service_BettransRng_MG_Settle_Set_V2", tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
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

                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleLc(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = GameType.MG_LiveCasino;
            var tpCategory = TpCategory.LiveCasino;

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var settleData = _partitionService.GetUnsettleDataList<SettleData_MG>("Service_BettransLC_MG_Settle_Get_V2", tpUnsettle);
                        var tpUserName = settleData.FirstOrDefault().Account;
                        var transId = settleData.FirstOrDefault().TransId;
                        var settleTime1 = settleData.Max(x => x.SettleTime);
                        var settleTimeMs = settleData.Max(x => x.SettleTimeNew);
                        var transTime1 = settleData.Min(x => x.TransTime);
                        var transTimeMs = settleData.Min(x => x.TransTimeNew);
                        var stake = settleData.Sum(x => x.Stake);
                        var validBet = settleData.Sum(x => x.ValidBet);
                        var winLostAmount = settleData.Sum(x => x.WinLostAmount);
                        var gameId = settleData.FirstOrDefault().GameId;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        if (tpCustInfo == null)
                        {
                            throw new UnsettleNotFoundException($"{tpUserName} TpCustInfo not found.");
                        }

                        long settleTime = convertTpBetRecordTime(settleTime1, settleTimeMs);
                        long transTime = convertTpBetRecordTime(transTime1, transTimeMs);

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = settleTime,
                            TransTime = transTime,
                            SettlementType = SettlementType.SettledForValid,
                            TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            GameID = gameId,
                        };

                        if (betRecordInfo.WinlostAmount == 0)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Draw;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: true);
                                _settledBLL.SetSettleFlag_BettransLC_MG("Service_BettransLC_MG_Settle_Set", tpUnsettle.TransId, transTime * 1000);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
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

                timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private long convertTpBetRecordTime(long creationTime, long creationTimeMs)
        {
            if (creationTime == 0 && creationTimeMs != 0)
            {
                return creationTimeMs / 1000;
            }

            long time = creationTime - new DateTime(1970, 1, 1).Ticks;
            time /= TimeSpan.TicksPerSecond;
            return time;
        }
    }
}
