using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
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
        private readonly MySqlClient _wagerDbClient;
        
        
        public HLWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<HLWorker>>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
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
            var gameType = GameType.HaoLong_Lottery;
            var tpCategory = TpCategory.Lottery;
            var logName = $"Settle_HaoLong";
            var getSP = $"Service_TpWager_HL_Settle_Get";
            var setSP = $"Service_TpWager_HL_Settle_Set";
            var dueTimeSpan = TimeSpan.FromSeconds(30);
            
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                //forContinue
                if (tpUnsettleList.Count() == 200)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }

                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { TransId = transId }, DataBase.Wager).First();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }
                        
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = unsettled.GameId,
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gameType, transId, true, DataBase.Wager);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        _logger.LogError(ex1, $"{logName} exMessage:{ex1.Message} || TransId : {transId}");
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
            }
            finally
            {
                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}