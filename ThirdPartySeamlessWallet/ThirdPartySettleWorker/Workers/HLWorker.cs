using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class HLWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.HaoLong;
        
        private Timer _timer;
        
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<HLWorker> _logger;
        private readonly string _tpName;
        
        public HLWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<HLWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoHLSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoHLSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = GameType.HaoLong_4D;
            var tpCategory = TpCategory.Lottery;
            var logName = $"Settle_HaoLong";
            var getSP = $"Service_TpWager_HL_Settle_Get_V2";
            var setSP = $"Service_TpWager_HL_Settle_Set";
            var dueTimeSpan = TimeSpan.FromMinutes(15);
            
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
                    
                    var getUnsettledList = _tpUnSettledBLL.GetTpUnsettleData<SettleDataHL>(getSP, new { wager.TransId }, DataBase.Wager);
                    if(getUnsettledList == null || !getUnsettledList.Any())
                    {
                        throw new UnsettleNotFoundException($"{_tpName} - HaoLong Can fount TransId : {wager.TransId}.");
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

                        var transType = unsettled.StrikePosition switch
                        {
                            "Void" => SettlementType.SettledForInvalid,
                            "0" => SettlementType.UnSettled,
                            _ => SettlementType.SettledForValid
                        };

                        //UnSettled先給預設狀態，後續結算後再更新資料
                        if (transType != SettlementType.UnSettled)
                        {

                            betRecordInfo = new BetRecordInfo
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
                                GameID = unsettled.GameId,
                            };
                        }
                        else
                        {
                            betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = unsettled.Stake,
                                TpUserName = unsettled.Account,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = 0,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                SettledTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                                TransTime = unsettled.TransTime,
                                SettlementType = transType,
                                GameID = unsettled.GameId,
                            };
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            if (transType != SettlementType.UnSettled)
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, wager.TransId, true, DataBase.Wager);
                            }
                            Task.Run(() =>
                            {
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
        
        private class SettleDataHL : SettleData
        {
            public string StrikePosition { get; set; }
        }
        
        #endregion
        
    }
}