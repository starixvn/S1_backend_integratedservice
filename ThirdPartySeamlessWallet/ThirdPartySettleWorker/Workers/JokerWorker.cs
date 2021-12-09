using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            timerRNG = new Timer(Settle, GameType.Joker_RNG, TimeSpan.Zero, TimeSpan.Zero);
            //timerFish = new Timer(Settle, GameType.Joker_Fish, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            timerFish?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void Settle(object state)
        {
            var forContinue = false;
            var isError = false;        
            var gametype = (GameType)state;
            
            var tpCategory = gametype == GameType.Joker_RNG ? TpCategory.Slots : TpCategory.Fish;              
            var getSP = $"Service_Bettrans{(gametype == GameType.Joker_RNG ? "Rng" : "Fish")}_Joker_Unsettle_Get";

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
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { TransId = transId },DataBase.Main).First();
                        
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, settleData.Account);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);
                        
                        if (tpCurrency.ExchangeRate > 0)
                        {
                            settleData.Stake *= tpCurrency.ExchangeRate;
                            settleData.ValidBet *= tpCurrency.ExchangeRate;
                            settleData.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)thisTp,
                            Stake = settleData.Stake,
                            ValidBet = settleData.ValidBet,
                            WinlostAmount = settleData.WinLostAmount,
                            TransId = settleData.TransId,
                            GameType = gametype,
                            TpUserName = settleData.Account,
                            SettledTime = settleData.SettleTime,
                            TransTime = settleData.TransTime,
                            SettlementType = _settledBLL.GetSettlementType(gametype, settleData.GameId),
                            TicketStatus = settleData.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
                        };
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(thisTp, gametype, transId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(thisTp, gametype, transId, isSuccess: false);
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
    }
}
