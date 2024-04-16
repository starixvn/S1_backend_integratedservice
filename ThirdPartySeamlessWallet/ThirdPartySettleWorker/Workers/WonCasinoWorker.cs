using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class WonCasinoWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.WonCasino;
        
        private Timer _timer;
        
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<WonCasinoWorker> _logger;
        private readonly string _tpName;
        
        public WonCasinoWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<WonCasinoWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWonCasinoSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoWonCasinoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = GameType.WonCasino_LiveCasino;
            var tpCategory = TpCategory.LiveCasino;
            var logName = $"Settle_WonCasino";
            var getSP = $"Service_Bettrans_WonCasino_Settle_Get";
            var setSP = $"Service_Bettrans_WonCasino_Settle_Set";
            var dueTimeSpan = TimeSpan.FromMinutes(1);
            
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                forContinue = tpUnsettleList.Count() == 200;

                foreach (var wager in tpUnsettleList)
                {
                    var getUnsettledList = _partitionService.GetUnsettleDataList<SettleDataWonCasino>(getSP, wager);
                    if(getUnsettledList == null || !getUnsettledList.Any())
                    {
                        throw new UnsettleNotFoundException($"{_tpName} - WonCasino Can fount TransId : {wager.TransId}.");
                    }
                    try
                    {
                        //拿最後一筆資料
                        var unsettled = getUnsettledList.LastOrDefault(x => x.TransId == wager.TransId);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var transType = TransTypeCheck(unsettled);
                        
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            TpUserName = unsettled.Account,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = transType,
                            GameID = ConvertGameType(unsettled.gametype),
                        };

                        if (betRecordInfo.SettlementType == SettlementType.SettledForInvalid)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(setSP, betRecordInfo);
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        isError = true;
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {wager.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, $"{logName} exMessage:{ex.Message}");
            }
            finally
            {
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(10);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }
                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        #region private

        private string ConvertGameType(int gametype)
        {
            return gametype switch
            {
                4 => "Baccarat",
                5 => "Roulette",
                10 => "DragonTiger",
                11 => "SicBo",
                18 => "ColorDisc",
                19 => "FishPrawnCrab",
                _ => "NA"
            };
        }

        private SettlementType TransTypeCheck(SettleDataWonCasino settleDataWonCasino)
        {
            var transType = (int)SettlementType.UnSettled;

            return settleDataWonCasino.type switch
            {
                (int)WonCasinoTypeEnum.Bet => settleDataWonCasino.TicketStatus switch
                {
                    (int)TicketStatus.UnSettled => SettlementType.UnSettled,
                    (int)TicketStatus.Refund => SettlementType.SettledForInvalid,
                    _ => SettlementType.SettledForValid
                },
                (int)WonCasinoTypeEnum.Cancel => SettlementType.SettledForInvalid,
                (int)WonCasinoTypeEnum.ReSettle => SettlementType.SettledForInvalid,
                _ => SettlementType.SettledForValid
            };
        }
        #endregion
        
        #region Model
        
        private class SettleDataWonCasino : SettleData
        {
            public int gametype { get; set; }
            public int type { get; set; }
        }
        
        #endregion
        }
}
