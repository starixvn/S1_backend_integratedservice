using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class EdayWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Eday;
        
        private Timer _timer;
        
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<EdayWorker> _logger;
        private readonly string _tpName;
        
        public EdayWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<EdayWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoEdaySettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoEdaySettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            
            var forContinue = false;
            var isError = false;
            var gameType = GameType.Eday_Lottery;
            var tpCategory = TpCategory.Lottery;
            var logName = $"Settle_Eday";
            var getSP = $"Service_BettransLT_Eday_Settle_Get";
            var getWager = $"Service_TpWager_Eday_Get";
            var setSP = $"Service_BettransLT_Eday_Settle_Set";
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
                    
                    var getUnsettledList = _tpUnSettledBLL.GetTpUnsettleData<SettleDataEday>(getSP, wager, DataBase.Main);
                    var wagerData = _tpUnSettledBLL.GetTpUnsettleData<SettleDataEday>(getWager,
                        new { TransId = wager.TransId }, DataBase.Wager).FirstOrDefault();
                   
                    if(getUnsettledList == null || !getUnsettledList.Any())
                    {
                        throw new UnsettleNotFoundException($"{_tpName} - Eday Can fount TransId : {wager.TransId}.");
                    }
                    
                    try
                    {
                        var betRecordInfo = new BetRecordInfo();
                        
                        var unsettled = getUnsettledList.FirstOrDefault(x => x.TransId == wager.TransId);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        //若拉單都還沒拉回來，先給預設UnSettled
                        var transType = SettlementType.UnSettled;
                        if (wagerData != null)
                        {
                            transType = wagerData.BillStatus switch
                            {
                                100 => SettlementType.SettledForInvalid,
                                10 => SettlementType.UnSettled,
                                200 => SettlementType.SettledForValid,
                                _ => SettlementType.SettledForInvalid
                            };
                        }
                        
                        if (transType != SettlementType.UnSettled)
                        {

                            betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = Math.Abs(unsettled.Stake),
                                TpUserName = unsettled.Account,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = transType,
                                GameID = unsettled.GameId,
                            };
                        }
                        else
                        {
                            betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = Math.Abs(unsettled.Stake),
                                TpUserName = unsettled.Account,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = 0,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = transType,
                                GameID = unsettled.GameId,
                            };
                        }
                        
                        if (transType == SettlementType.SettledForInvalid)
                        {
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            
                            Task.Run(() =>
                            {
                                //TpWager有抓到注單回來並且有狀態才SetSettle
                                if (transType != SettlementType.UnSettled)
                                {
                                    _ = _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, wager.TransId);
                                }
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
        
        #region Model
        
        private class SettleDataEday : SettleData
        {
            public string billId { get; set; }
            public int BillStatus { get; set; }
            public int BetType { get; set; }
        }
        
        #endregion
    
    }
}