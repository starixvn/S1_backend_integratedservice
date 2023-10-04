using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class WMWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.WM;
        private Timer _timerLC;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ILogger<WMWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly string _tpName;
        
        public WMWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _logger = provider.GetRequiredService<ILogger<WMWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerLC = new Timer(DoSettleWMWork, GameType.WM_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleWMWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gameType = (GameType)state;
            var tpId = (int)Tp.WM;
            var logName = "WMDoSettleLiveCasinoWork";
            
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    var unsettledData = _settledBLL.GetUnsettled_WM(tpUnsettle.TransId);
                    var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData_WM>("Service_TpWagerWM_Settle_Get",
                        new { TransId = tpUnsettle.TransId }, DataBase.Wager).FirstOrDefault();
                        if (settleData != null)
                        {
                            try
                            {
                                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, settleData.Account);
                                var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    settleData.Stake *= tpCurrency.ExchangeRate;
                                    settleData.ValidBet *= tpCurrency.ExchangeRate;
                                    settleData.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (settleData.Stake == unsettledData.RealBetAmount && settleData.WinLostAmount == unsettledData.WinLostAmount)
                                {
                                    var betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = tpId,
                                        Stake = settleData.Stake,
                                        ValidBet = settleData.ValidBet,
                                        WinlostAmount = settleData.WinLostAmount,
                                        TransId = settleData.TransId,
                                        GameType = GameType.WM_LiveCasino,
                                        TpUserName = settleData.Account,
                                        SettledTime = settleData.SettleTime,
                                        TransTime = settleData.TransTime,
                                        SettlementType = SettlementType.SettledForValid,
                                        GameID = settleData.GameId,
                                    };

                                    _ = _settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);
                                    
                                    Task.Run(() =>
                                    {
                                        _settledBLL.SetSettleFlag_TpWagerWM(tpUnsettle.TransId);
                                        _settledBLL.SetSettleFlag_BettransLC_WM(tpUnsettle.TransId);
                                        _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, tpUnsettle.TransId, true);
                                    });
                                    
                                }

                            }
                            catch (Exception ex1)
                            {
                                isError = true;
                                _logger.LogError($"{JsonConvert.SerializeObject(unsettledData)}\r\n{ex1.ToString()}", logName);
                            }
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

                _timerLC?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}