using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class APWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.AP; }

        private Timer timerRNG;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<APWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;
        private readonly ThirdPartyBLL _thirdPartyBLL;


        public APWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<APWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRNG = new Timer(Settle, GameType.AP_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRNG?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void Settle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = (GameType)state;
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gametype} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        //Get all Bettrans data
                        var UnsettleData = _partitionService.GetUnsettleDataList<SettleDataRNG_AP>("Service_BettransRng_AP_Unsettle_Get", tpUnsettle);                         
                        var settleData = UnsettleData.Where(i => i.TicketType == (int)BettransType.EndRound);
                        var betData = UnsettleData.Where(i => i.TicketType == (int)BettransType.Bet);

                        var tpUserName = settleData.FirstOrDefault().playerid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var stake = settleData.Sum(x => x.totalstake);
                        var winLostAmount = settleData.Sum(x => x.totalwin) + settleData.Sum(x => x.jpwin) - stake;
                        var settledTime = settleData.Where(x => x.settletime > 0).FirstOrDefault().settletime;
                        var betTime = betData.Where(x => x.bettime > 0).FirstOrDefault().bettime;
                        var result = _settledBLL.Settled(tpCustInfo, new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = stake,
                            WinlostAmount = winLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = settledTime,
                            TransTime = betTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, settleData.FirstOrDefault().gamecode),
                            TicketStatus = winLostAmount > 0 ? TicketStatus.Win : winLostAmount == 0 ? TicketStatus.Draw : TicketStatus.Lost
                        },  TpCategory.Slots);

                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, isSuccess: false);
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

                timerRNG?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
