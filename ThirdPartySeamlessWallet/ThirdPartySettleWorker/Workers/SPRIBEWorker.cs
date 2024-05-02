using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class SPRIBEWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.SPRIBE;
        private Timer timerRngByRound;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly PartitionService _partitionService;
        private readonly IIMBLL _imBll;
        private readonly ILogger<SPRIBEWorker> _logger;
        private readonly string _tpName;

        public SPRIBEWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _partitionService = provider.GetRequiredService<PartitionService>();
            _imBll = provider.GetRequiredService<IIMBLL>();
            _logger = provider.GetRequiredService<ILogger<SPRIBEWorker>>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerRngByRound = new Timer(DoSettleRngWork, GameType.SPRIBE_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerRngByRound?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettleRngWork(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp;
            var gameType = (GameType)state;
            var category = TpCategory.Slots;
            var bettransGet = "Service_BettransRng_SPRIBE_Settle_Round_Get";
            var bettransSet = "Service_BettransRng_SPRIBE_Settle_Round_Set";
            var tpWagerSet = "Service_TpWagerRng_SPRIBE_Settle_Set";
            var logName = "SpribeDoSettleRngWork";

            try
            {
                var tpUnsettleList = _tpUnSettledBLL.GetTpUnsettleList(Tp, gameType);
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} {gameType} Round no unsettle data.");
                }

                forContinue = tpUnsettleList.Count() == 200;
                foreach (var unsettled in tpUnsettleList)
                {
                    try
                    {
                        // round 只會有單筆，但有可能跨月
                        var betTrans = _partitionService.GetUnsettleDataList<SettleTransactions_IMSlot>(bettransGet, unsettled);
                        if (!betTrans.Any())
                        {
                            continue;
                        }

                        var betTransData = betTrans.First();
                        var gameId = betTransData.GameId;
                        var transId = betTransData.RoundId;
                        var stake = betTrans.Sum(x => x.Stake);
                        var validBet = betTrans.Sum(x => x.ValidBet);
                        var winLostAmount = betTrans.Sum(x => x.WinLostAmount);
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, betTransData.PlayerId);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        if (tpCurrency == null)
                        {
                            _logger.LogError($"{logName} {betTransData.PlayerId} TpCurrency not found.");
                            continue;
                        }

                        var tpWagerData = _tpUnSettledBLL.GetTpUnsettleData<WagerDataIM>("Service_TpWagerRng_SPRIBE_Unsettle_Get", new { RoundId = transId }, DataBase.Wager).FirstOrDefault();

                        var settleAmountDataIm = new SettleAmountDataIM
                        {
                            Stake = betTransData.Stake,
                            ValidBet = betTransData.ValidBet,
                            WinLostAmount = betTransData.WinLostAmount
                        };

                        if (tpWagerData is not null)
                        {
                            if (!IsBetTransSameAsTpWager(settleAmountDataIm, tpWagerData, tpCurrency))
                            {
                                _logger.LogError($"transId: {transId} is not equal " +
                                                 $"settle: {JsonConvert.SerializeObject(betTransData)} " +
                                                 $"wager: {JsonConvert.SerializeObject(tpWagerData)}");
                            }
                        }
                        else
                        {
                            continue;
                        }

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = thisTp,
                            Stake = stake,
                            ValidBet = validBet,
                            WinlostAmount = winLostAmount,
                            TransId = transId,
                            GameType = gameType,
                            TpUserName = betTransData.PlayerId,
                            SettledTime = new DateTimeOffset(betTrans.Last().TimeStamp).ToUnixTimeSeconds(),
                            TransTime = new DateTimeOffset(tpWagerData.BetDate).ToUnixTimeSeconds(),
                            SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType((int)Tp, gameId),
                            GameID = betTransData.GameId
                        };

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, transId, isSuccess: true);
                                _ = _imBll.SetSettleFlag_BettransRng_IM(bettransSet, transId, betTrans.Last().TimeStamp);
                                _ = _imBll.SetSettleFlag_TpWager_IM(tpWagerSet, transId);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        LogContext.PushProperty("TP", _tpName);
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, gameType, unsettled.TransId, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"{logName}:{JsonConvert.SerializeObject(unsettled)}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"{logName}:{ex.Message}");
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerRngByRound.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
        {
            var dueTimeSpan = TimeSpan.FromMinutes(1);

            if (forContinue)
            {
                dueTimeSpan = TimeSpan.FromSeconds(1);
            }
            else if (isError)
            {
                dueTimeSpan = TimeSpan.FromMinutes(1);
            }

            return dueTimeSpan;
        }

        private bool IsBetTransSameAsTpWager(SettleAmountDataIM betTrans, WagerDataIM wager, TpCurrency tpCurrency)
        {
            if (tpCurrency.ExchangeRate <= 0) return false;

            // 加入幣別匯率轉換，tpWager 是原始資料
            var wagerStake = wager.StakeAmount * tpCurrency.ExchangeRate;
            var wagerValidBet = wager.BetAmount * tpCurrency.ExchangeRate;
            decimal? wagerWinLostAmount = wager.WinLoss * tpCurrency.ExchangeRate;

            return wagerStake == betTrans.Stake
                   && wagerValidBet == betTrans.ValidBet
                   && wagerWinLostAmount == betTrans.WinLostAmount;
        }
    }
}
