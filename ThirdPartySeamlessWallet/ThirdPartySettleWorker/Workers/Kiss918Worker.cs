using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    internal class Kiss918Worker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.Kiss918;

        private Timer _timer;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<Kiss918Worker> _logger;
        private readonly string _tpName;
        private readonly PartitionService _partitionService;
        private readonly TpRoundGenService _tpRoundGenService;

        public Kiss918Worker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<Kiss918Worker>>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpRoundGenService = provider.GetRequiredService<TpRoundGenService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoSettle, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettle(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var gametype = GameType.Kiss918_RNG;
            var tpCategory = TpCategory.Slots;
            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gametype);
                var unsettleList = tpUnsettleList.ToList();
                if (!unsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                forContinue = unsettleList.Count() == 200;
                foreach (var tpUnsettle in unsettleList)
                {
                    try
                    {
                        var unsettleData = _partitionService.GetUnsettleDataList<SettleData>("Service_BettransRng_Kiss918_Unsettle_Get", tpUnsettle).FirstOrDefault();
                        if (unsettleData != null)
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCacheByTpCustId((int)Tp, unsettleData.Account);
                            if (tpCustInfo == null)
                            {
                                throw new UnsettleNotFoundException($"{unsettleData.Account} TpCustInfo not found.");
                            }

                            //TODO：等wager做好，要重新比對TransTime和SettleTime，還有檢查是否為refund/jackpot，如果發現該筆帳wager沒有則？？？

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp,
                                Stake = unsettleData.Stake,
                                ValidBet = unsettleData.ValidBet,
                                WinlostAmount = unsettleData.WinLostAmount,
                                TransId = tpUnsettle.TransId,
                                GameType = gametype,
                                TpUserName = tpCustInfo.TpUserName,
                                SettledTime = unsettleData.SettleTime,
                                TransTime = unsettleData.TransTime,
                                SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, unsettleData.GameId),
                                TicketStatus = SetTicketStatus(unsettleData.WinLostAmount)
                            };

                            betRecordInfo.ReferenceNo = GetRoundId(unsettleData.TransId);

                            var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, true);
                                });
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, tpUnsettle.TransId, false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"TransId:{tpUnsettle.TransId} settle failed, exMessage:{ex1.Message}");
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

                _timer?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private TicketStatus SetTicketStatus(decimal winLostAmount)
        {
            if (winLostAmount == 0) return TicketStatus.Draw;

            if (winLostAmount > 0) return TicketStatus.Win;

            return TicketStatus.Lost;
        }

        private string GetRoundId(string playSessionID)
        {
            var existDatas = _tpRoundGenService.GetExistRoundId(Tp, GameType.Kiss918_RNG, playSessionID);

            if (existDatas.Any())
                return existDatas.First().SQ.ToString();
            else
                throw new Exception("roundId not found.");
        }

    }
}
