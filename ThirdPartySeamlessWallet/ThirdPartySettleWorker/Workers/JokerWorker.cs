using Dapper;
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
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;

namespace ThirdPartySettleWorker.Workers
{
    public class JokerWorker : BackgroundService
    {
        private Timer timerRNG;
        private Timer timerFish;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<JokerWorker> _logger;
        private readonly Tp thisTp = Tp.Joker;
        private readonly string _tpName;
        

        public JokerWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<JokerWorker>>();
            _tpName = Enum.GetName(typeof(Tp), thisTp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(Test, GameType.Joker_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerFish = new Timer(Test, GameType.Joker_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void Test(object state)
        {
            _logger.LogInformation($"gametype:{(GameType)state} || {DateTime.Now:yyyy:mm:dd}");
        }
        private void Settle(object state)
        {
            var forContinue = false;
            var isError = false;
           
            var gametype = (GameType)state;
            var tpCategory = gametype == GameType.Joker_RNG ? TpCategory.Slots : TpCategory.Fish;          
            var type = gametype == GameType.Joker_RNG ? "Rng" : "Fish";         
            var getSP = $"Service_Bettrans{type}_Joker_Unsettle_Get";
            var setSP = $"Service_Bettrans{type}_Joker_Settle_Set";

            try
            {
                var tpUnsettleTransIdList = _tpUnSettledBLL.GetTpUnsettleTransIdList(thisTp, gametype);
                if (!tpUnsettleTransIdList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleTransIdList.Count() == 200;
                foreach (var transId in tpUnsettleTransIdList)
                {
                    try
                    {
                        var unsettled = _tpUnSettledBLL.GetTpUnsettleData<JokerSettleData>(getSP, new { TransId = transId },DataBase.Main).First();
                        
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, unsettled.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);
                        
                        if (tpCurrency.ExchangeRate > 0)
                        {
                            unsettled.Stake *= tpCurrency.ExchangeRate;
                            unsettled.ValidBet *= tpCurrency.ExchangeRate;
                            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)thisTp,
                            Stake = unsettled.Stake,
                            ValidBet = unsettled.ValidBet,
                            WinlostAmount = unsettled.WinLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gametype,
                            TpUserName = unsettled.Account,
                            SettledTime = unsettled.SettleTime,
                            TransTime = unsettled.TransTime,
                            SettlementType = _settledBLL.GetSettlementType(gametype, unsettled.GameCode),
                            TicketStatus = unsettled.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetSettleFlagBySpName(setSP, unsettled.TransId);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetSettleFlagBySpName(setSP, transId, isSuccess: false);
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
                TimeSpan dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _ = gametype == GameType.Joker_RNG ?
                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero) :
                timerFish?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public class JokerSettleData : SettleData
        {
            public string GameCode { get; set; }
        }
    }
}
