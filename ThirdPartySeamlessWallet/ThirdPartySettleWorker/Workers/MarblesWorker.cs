using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class MarblesWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Marbles;
        
        private Timer _timer;
        
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<MarblesWorker> _logger;
        private readonly string _tpName;
        
        public MarblesWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetRequiredService<ILogger<MarblesWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoMarblesSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoMarblesSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = GameType.Marbles_Lottery;
            var tpCategory = TpCategory.Lottery;
            var logName = $"Settle_Marbles";
            var getSP = $"Service_BettransLT_Marbles_Settle_Get";
            var setSP = $"Service_BettransLT_Marbles_Settle_Set";
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
                    var getUnsettledList = _partitionService.GetUnsettleDataList<SettleData>(getSP, wager);
                    if(getUnsettledList == null || !getUnsettledList.Any())
                    {
                        throw new UnsettleNotFoundException($"{_tpName} - Can fount TransId : {wager.TransId}.");
                    }
                    try
                    {
                        var getSettleWager = wager.TransId + "-1";
                        var transType = SettlementType.SettledForValid;
                        //因為三方的Bettrans沒有狀態能確認
                        //故目前先以是否有抓回注單作為判斷 一律先以未結算單下去做
                        //若抓回來會再次寫入TpUnsettle 再次結算
                        var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<TpWagerMarblesModel>(getWagerSP, new { TransId = wager.TransId }, DataBase.Wager);
                        if (!tpWagerData.Any())
                        {
                            transType = SettlementType.UnSettled;
                            getSettleWager = wager.TransId;
                        }
                        
                        var unsettled = getUnsettledList.FirstOrDefault(x => x.TransId == getSettleWager);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            TpUserName = unsettled.Account,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            //前面拿結算資料 寫入資料的時候拿局號當作結算單
                            TransId = wager.TransId,
                            GameType = gameType,    
                            SettledTime = unsettled.SettleTime / 1000,
                            TransTime = unsettled.TransTime / 1000,
                            SettlementType = transType,
                            GameID = unsettled.GameId,
                        };

                        if (betRecordInfo.SettlementType == SettlementType.SettledForInvalid)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                                
                            Task.Run(() =>
                            {
                                _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(setSP, betRecordInfo);
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, wager.TransId, isSuccess: true);
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
    }
}