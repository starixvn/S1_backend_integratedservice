using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class AllbetWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.Allbet; }
        private Timer timerLive;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<AllbetWorker> _logger;
        private readonly string _tpName;
        private readonly MySqlClient _mainDbClient;

        public AllbetWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<AllbetWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {  
            timerLive = new Timer(DoSettle, GameType.Allbet_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerLive?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var gameType = (GameType)state;
            var tpCategory = TpCategory.LiveCasino;
            var logName = $"Settle_AllBet";
            var getSP = $"Service_TpWagerAllBet_Settle_Get_V2";
            var setSP = $"Service_TpWagerAllBet_Settle_Set_V2";       
            var checkSP = $"Service_BettransLC_Allbet_Settle_Get";
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
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<AllbetSettleData>(getSP, new { TransId = transId }, DataBase.Wager).First();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            unsettled.DepositAmount *= tpCurrency.ExchangeRate;
                        }

                        var check = _mainDbClient.QueryFirst<CheckData>(checkSP, new { BetNum = transId, Client = unsettled.Account });
                        if ((check.Stake - unsettled.DepositAmount) != unsettled.Stake || (check.SettleAmount - check.Stake) != unsettled.WinLostAmount)
                        {
                            _logger.LogWarning($"Settle Amount error || wager : {JsonConvert.SerializeObject(unsettled)} || bettrans : {JsonConvert.SerializeObject(check)}");
                            continue;
                        }
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.Stake,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = SettlementType.SettledForValid
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
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, false);
                        });
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
                timerLive?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }

    public class CheckData
    { 
        public decimal SettleAmount { get; set; }
        public decimal Stake { get; set; }
    }

    public class AllbetSettleData : SettleData
    {
        public decimal DepositAmount { get; set; }
    }
}
