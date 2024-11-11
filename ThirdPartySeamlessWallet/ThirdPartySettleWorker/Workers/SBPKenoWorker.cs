using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    internal class RNGKenoWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.RNGKeno; }

        private Timer _timer;
        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly PartitionService _partitionService;
        private readonly ILogger<RNGKenoWorker> _logger;
        private readonly string _tpName;

        public RNGKenoWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<RNGKenoWorker>>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoSettle, GameType.RNG_Keno, TimeSpan.Zero, TimeSpan.Zero);
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
            var gameType = (GameType)state;
            const string bettransGet = "Service_BettransRng_Keno_Settle_Get";
            const string bettransSet = "Service_BettransRng_Keno_Settle_Set";
            const string logName = $"Settle_SBP_Keno";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        var transId = unsettled.TransId;

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:bettrans not found ({gameType})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var tpUserName = bettrans.FirstOrDefault().Account;
                        var stake = bettrans.Sum(x => x.Stake);
                        var validBet = bettrans.Sum(x => x.ValidBet);
                        var winLostAmount = bettrans.Sum(x => x.WinLostAmount);
                        var gameId = bettrans.FirstOrDefault().GameId;
                        var settledTime = bettrans.Max(x => x.SettleTime);
                        var transTime = bettrans.Min(x => x.TransTime);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = tpUserName,
                            SettledTime = settledTime,
                            TransTime = transTime,
                            SettlementType = SettlementType.SettledForValid,
                            GameID = gameId,
                        };

                        var result = _settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                        if (result == 1)
                        {
                            SetSettleFlag(bettransSet, gameType, betRecordInfo);
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message} || TransId : {unsettled.TransId}");
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
                var dueTimeSpan = TimeSpan.FromSeconds(30);
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

        private void SetSettleFlag(string bettransSet, GameType gametype, BetRecordInfo betRecordInfo)
        {
            _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, betRecordInfo.TransId, true);
            _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(bettransSet, betRecordInfo);
        }
    }
}