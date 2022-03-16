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
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class SAWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.SA;

        private Timer timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<SAWorker> _logger;
        private readonly string _tpName;


        public SAWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<SAWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoSettle, GameType.SA_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            var tpCategory = TpCategory.LiveCasino;

            var logName = $"SettleSA";
            var getSP = $"Service_TpWagerSA_Settle_Get_V2";
            var setSP = $"Service_TpWagerSA_Settle_Set_V2";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var transId in tpUnsettleList.Select(tpUnsettle => tpUnsettle.TransId))
                {
                    try
                    {
                        var split = transId.Split("_");
                        var gameId = split[0];
                        var userName = split[1];
                        
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleData>(getSP, new { GameID = gameId, Username = userName }, DataBase.Wager).First();
                        var betTrans = _settledBLL.GetSettle_BettransLC_SA(gameId, userName);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, userName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate > 1)
                        {
                            settleData.Stake *= tpCurrency.ExchangeRate;
                            settleData.ValidBet *= tpCurrency.ExchangeRate;
                            settleData.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        if (betTrans.Stake == settleData.Stake && betTrans.WinLostAmount == settleData.WinLostAmount)
                        {
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = settleData.Stake,
                                ValidBet = settleData.ValidBet,
                                WinlostAmount = settleData.WinLostAmount,
                                TransId = transId,
                                GameType = gametype,
                                TpUserName = userName,
                                SettledTime = settleData.SettleTime,
                                TransTime = settleData.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };
                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                _tpUnSettledBLL.SetSettleFlag(setSP, new { GameId = gameId, UserName = userName },
                                    DataBase.Wager);
                            }
                        }

                       
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetSettleFlag(setSP, Tp, gametype, transId, false);
                        });
                        isError = true;
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
                isError = true;
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromSeconds(30);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
