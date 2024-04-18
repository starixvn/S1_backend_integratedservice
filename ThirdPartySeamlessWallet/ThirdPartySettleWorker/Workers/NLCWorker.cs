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
    internal class NLCWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.NLC;
        private Timer _timerSettle;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<NLCWorker> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly PartitionService _partitionService;
        private readonly string _tpName;

        public NLCWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<NLCWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSettle = new Timer(DoSettleWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSettle?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp;
            var gameType = GameType.NLC_RNG;
            var tpCategory = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_NLC_Settle_Get";
            var bettransSet = "Service_BettransRng_NLC_Settle_Set";
            var wagerGet = "Service_TpWagerRng_NLC_Settle_Get";
            var logName = "Settle_NLC";

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
                        string TransId = unsettled.TransId;

                        var bettrans = _partitionService.GetUnsettleDataList<SettleData>(bettransGet, unsettled);
                        if (!bettrans.Any())
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:bettrans not found ({gameType})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var wager = _tpUnSettledBLL.GetTpUnsettleData<SettleData_NLC>(wagerGet, new { TransId = TransId }, DataBase.Wager).FirstOrDefault();
                        if (wager == null)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:wager not found ({gameType})\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var tpUserName = bettrans.FirstOrDefault().Account;
                        var stake = bettrans.Sum(x => x.Stake);
                        var validBet = bettrans.Sum(x => x.ValidBet);
                        var winLostAmount = bettrans.Sum(x => x.WinLostAmount);
                        var gameId = bettrans.FirstOrDefault().GameId;

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, tpUserName);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency.ExchangeRate != 0)
                        {
                            wager.Stake *= tpCurrency.ExchangeRate;
                            wager.WinLostAmount *= tpCurrency.ExchangeRate;
                        }

                        var dWagerStake = Math.Truncate(wager.Stake * 10) / 10;
                        var dBettransStake = Math.Truncate(stake * 10) / 10;
                        var dDiffStake = Math.Abs(dWagerStake - dBettransStake);

                        if (dDiffStake > 0.5m)
                        {
                            LogContext.PushProperty("TP", _tpName);
                            _logger.LogWarning($"{logName}:amount not match\r\n{JsonConvert.SerializeObject(wager)}\r\n{JsonConvert.SerializeObject(unsettled)}");
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = unsettled.TransId,
                            GameType = gameType,
                            TpUserName = tpUserName,
                            SettledTime = wager.SettleTime,
                            TransTime = wager.TransTime,
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, gameId),
                            GameID = gameId,
                        };

                        if (string.Equals(wager.status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            SetSettleFlag(bettransSet, gameType, betRecordInfo);
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        isError = true;
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, $"{logName}:{ex.Message}");
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                _timerSettle.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void SetSettleFlag(string bettransSet, GameType gametype, BetRecordInfo betRecordInfo)
        {
            _ = _tpUnSettledBLL.SetTpUnsettle(Tp, gametype, betRecordInfo.TransId, true);
            _ = _settledBLL.SetSettleFlagOfMonthlyBettrans(bettransSet, betRecordInfo);
        }
    }
}
