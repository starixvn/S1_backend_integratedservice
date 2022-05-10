using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Interfaces;

namespace ThirdPartySettleWorker.Workers
{
    public class PTWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp => Tp.PT;

        private Timer timerSettlePTRng;
        private Timer timerSettlePTLC;

        private readonly SettledBLL _settledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<PTWorker> _logger;
        private readonly string _tpName;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public PTWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<PTWorker>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpName = Enum.GetName(typeof(Tp), Tp);
            LogContext.PushProperty("TP", _tpName);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettlePTRng = new Timer(DoSettlePTRngWork, GameType.PT_RNG, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePTLC = new Timer(DoSettlePTLCWork, GameType.PT_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSettlePTRng?.Change(Timeout.Infinite, 0);
            timerSettlePTLC?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoSettlePTRngWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.PT;
            var gameType = GameType.PT_RNG;
            var logName = "Settle_PT_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListRng_PT();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 0) SetUnSettled(unsettled, tpCurrency);

                            var betRecordInfo = GetBetRecordInfo(thisTp, unsettled, gameType);
                            betRecordInfo.SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId);
                            
                            if (unsettled.Stake == 0 && unsettled.ValidBet == 0 && unsettled.WinLostAmount == 0)
                            {
                                _settledBLL.SetSettleFlag_TpWagerRng_PT(long.Parse(unsettled.TransId));
                            }
                            else
                            {
                                _settledBLL.Settled(tpCustInfo, betRecordInfo, PromotionCategory.PTSlotsGame);
                                _settledBLL.SetSettleFlag_TpWagerRng_PT(long.Parse(unsettled.TransId));
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerSettlePTRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePTLCWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.PT;
            var gameType = GameType.PT_LiveCasino;
            var logName = "Settle_PT_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = _settledBLL.GetUnsettledListLC_PT();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = _thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);

                            if (tpCurrency.ExchangeRate > 0) SetUnSettled(unsettled, tpCurrency);

                            var betRecordInfo = GetBetRecordInfo(thisTp, unsettled, gameType);
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                            
                            if (unsettled.Stake == 0 && unsettled.ValidBet == 0 && unsettled.WinLostAmount == 0)
                            {
                                _settledBLL.SetSettleFlag_TpWagerLC_PT(long.Parse(unsettled.TransId));
                            }
                            else
                            {
                                _settledBLL.Settled(tpCustInfo, betRecordInfo, PromotionCategory.PTLiveCasino);
                                _settledBLL.SetSettleFlag_TpWagerLC_PT(long.Parse(unsettled.TransId));
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            _logger.LogError($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                _logger.LogError(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                var dueTimeSpan = GetDueTimeSpan(forContinue, isError);
                timerSettlePTLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private static BetRecordInfo GetBetRecordInfo(int thisTp, SettleData unsettled, GameType gameType)
        {
            var betRecordInfo = new BetRecordInfo
            {
                TpId = thisTp,
                Stake = unsettled.Stake,
                ValidBet = unsettled.ValidBet,
                WinlostAmount = unsettled.WinLostAmount,
                ProgressiveBet = unsettled.ProgressiveBet,
                ProgressiveWin = unsettled.ProgressiveWin,
                TransId = unsettled.TransId,
                GameType = gameType,
                TpUserName = unsettled.Account,
                SettledTime = unsettled.SettleTime,
                TransTime = unsettled.SettleTime,
            };
            return betRecordInfo;
        }

        private static void SetUnSettled(SettleData unsettled, TpCurrency tpCurrency)
        {
            unsettled.Stake *= tpCurrency.ExchangeRate;
            unsettled.ValidBet *= tpCurrency.ExchangeRate;
            unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
            unsettled.ProgressiveBet *= tpCurrency.ExchangeRate;
            unsettled.ProgressiveWin *= tpCurrency.ExchangeRate;
        }

        private static TimeSpan GetDueTimeSpan(bool forContinue, bool isError)
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

            return dueTimeSpan;
        }
    }
}
