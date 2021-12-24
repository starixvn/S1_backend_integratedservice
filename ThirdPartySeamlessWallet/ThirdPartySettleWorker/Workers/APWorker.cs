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
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<APWorker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;


        public APWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<APWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
            _partitionService = provider.GetRequiredService<PartitionService>();
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
                        var settleData = _partitionService.GetUnsettleDataList<SettleDataRNG_AP>("Service_BettransRng_AP_Unsettle_Get", tpUnsettle)
                            .Where(i => i.TicketType == (int)BettransType.EndRound).Select(x=> x);

                        var winLostAmount = settleData.Sum(x => x.totalwin) + settleData.Sum(x => x.jpwin);
                        var tpUserName = settleData.FirstOrDefault().playerid;
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        var result = _settledBLL.Settled(tpCustInfo, new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = settleData.Sum(x=> x.totalstake),
                            ValidBet = settleData.Sum(x => x.totalstake),
                            WinlostAmount = winLostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = gametype,
                            TpUserName = tpUserName,
                            SettledTime = settleData.Where(x=> x.settletime > 0).FirstOrDefault().settletime / 1000,
                            TransTime = settleData.Where(x => x.settletime > 0).FirstOrDefault().settletime / 1000,
                            SettlementType = SettlementType.SettledForValid,
                            TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
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
