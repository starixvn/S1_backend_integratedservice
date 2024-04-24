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
            var getWagerSP = $"Service_TpWager_WonCasino_Get";
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
                        //此處去拿TpWager_WonCasino，Bettrans無資料，但有抓回來注單。
                        //state為-3則代表異常錯誤
                        //此單並無在我方下注成功，也未打Rollback
                        //確認後直接刪單，以免卡住TpUnSettle
                        var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<TpWagerWonCasinoModel>(getWagerSP, new { TransId = wager.TransId }, DataBase.Wager).FirstOrDefault();
                        if (tpWagerData.state != -3)
                        {
                            throw new UnsettleNotFoundException(
                                $"{_tpName} - WonCasino Can fount TransId : {wager.TransId}.");
                        }
                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, wager.TransId, isSuccess: true);
                        continue;
                    }
                    try
                    {
                        //拿最後一筆資料
                        var unsettled = getUnsettledList.LastOrDefault(x => x.TransId == wager.TransId);
                        
                        //Void單結算不處理
                        //經詢問type == 7 只有 玩家手動撤銷注單以及後台手動撤單(PRD 總代帳號才有權限)，
                        //為不影響STATEMENT顯示，固直接刪單不處理。
                        if (unsettled.type == 7)
                        {
                            _logger.LogInformation($"{logName} , TransId : {wager.TransId} , Void Order");
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: true);
                            continue;
                        }

                        //settled的 輸單stake會是0，會導致沒有Turnover，已ValidBet取代;
                        if (unsettled.type == 9)
                        {
                            if (unsettled.Stake == 0.00m && unsettled.WinLostAmount < 0)
                            {
                                unsettled.Stake = unsettled.ValidBet;
                                _logger.LogInformation($"{logName} , TransId : {wager.TransId} , Lose , Stake = {unsettled.Stake}");
                            }
                        }
                        
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

                        //deduct rollback可能沒有ValidBet，改用Amount
                        if (unsettled.ValidBet == 0.00m)
                        {
                            betRecordInfo.ValidBet = unsettled.Stake;
                        }

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
                (int)WonCasinoTypeEnum.ReSettle => SettlementType.SettledForValid,
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
        
        public class TpWagerWonCasinoModel
        {
            public int id { get; set; }
            public int userid { get; set; }
            public string username { get; set; }
            public int gameid { get; set; }
            public int gameroundid { get; set; }
            public int gameroundno { get; set; }
            public string gameresult { get; set; }
            public int state { get; set; }
            public int bettype { get; set; }
            public string betpoint { get; set; }
            public decimal betamount { get; set; }
            public decimal winlost { get; set; }
            public decimal betodds { get; set; }
            public DateTime updatetime { get; set; }
            public DateTime createtime { get; set; }
        }
        
        #endregion
        }
}
