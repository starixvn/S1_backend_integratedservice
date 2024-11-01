using Newtonsoft.Json;
using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PPWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.PP; }

        private Timer timerRNG;
        private Timer timerLC;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<PPWorker> _logger;
        private readonly PartitionService _partitionService;
        private readonly string _tpName;


        public PPWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<PPWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(DoSettle, GameType.PP_RNG, TimeSpan.Zero, TimeSpan.Zero);
           // timerLC = new Timer(DoSettle, GameType.PP_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = gametype == GameType.PP_RNG ? TpCategory.Slots : TpCategory.LiveCasino;
            var type = gametype == GameType.PP_RNG ? "Rng" : "LC";
            var logName = $"Settle_PP_{type}";
            var getSP = $"Service_Bettrans{type}_PP_Settle_Get";
            var setSP = $"Service_Bettrans{type}_PP_Settle_Set";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(getSP, unsettled);
                        var bettran = bettrans.FirstOrDefault();
                        if (!bettrans.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:bettrans not found ({gametype})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }
                        var tpUserName = bettran?.Account;
                        var stake = bettrans.Sum(x => x.Stake);
                        var validBet = bettrans.Sum(x => x.ValidBet);
                        var winLostAmount = bettrans.Sum(x => x.WinLostAmount);
                        var gameId = bettran?.GameId;
                        var settledTime = bettrans.Max(x => x.SettleTime);
                        var transTime = bettrans.Min(x => x.TransTime);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        if (gametype == GameType.PP_LiveCasino)
                        {
                            if (winLostAmount >= 0 && winLostAmount < stake)
                            {
                                validBet = winLostAmount;
                            }
                            else if (winLostAmount < 0)
                            {
                                validBet = Math.Abs(winLostAmount);
                            }
                        }

      
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = settledTime,
                            TransTime = transTime,
                            SettlementType = gametype == GameType.PP_LiveCasino ?
                            SettlementType.SettledForValid : _settledBLL.GetSettlementType(gametype, gameId),
                            TicketStatus = (TicketStatus)bettran?.TicketStatus,
                            GameID = gameId
                        };

                        //if (betRecordInfo.WinlostAmount == 0)
                        //{
                        //    betRecordInfo.TicketStatus = TicketStatus.Draw;
                        //}
                        //// check refund
                        //if (unsettled.TicketStatus > 0)
                        //{
                        //    betRecordInfo.TicketStatus = TicketStatus.Refund;
                        //}

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, betRecordInfo.TransId, true);
                                _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(setSP, betRecordInfo);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, unsettled.TransId, true);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {unsettled.TransId}");
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

                _ = gametype == GameType.PP_RNG ?
                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero) :
                timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
