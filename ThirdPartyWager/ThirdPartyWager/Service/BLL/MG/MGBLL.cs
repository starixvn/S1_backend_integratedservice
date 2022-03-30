using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.MG
{
    public class MGBLL : IMGBLL
    {
        private readonly IApiMGBLL _mgApi;
        private readonly IApiSWBLL _swApi;
        private readonly List<string> _tpCurrency = new List<string> { "VND", "USD", "INR", "IDR", "MYR", "THB" };
        private readonly ILogger<MGBLL> _logger;
        private readonly FincanceService _fincanceService;
        private Tp tp = Tp.MG;
        private Dictionary<string, int> _currencyId = new Dictionary<string, int>();

        public MGBLL(IServiceProvider provider)
        {
            _mgApi = provider.GetRequiredService<IApiMGBLL>();
            _swApi = provider.GetRequiredService<IApiSWBLL>();
            var config = provider.GetRequiredService<ThirdPartyBLL>().GetThirdPartySetting((int)tp);
            _mgApi.MgConfig = config;
            _logger = provider.GetRequiredService<ILogger<MGBLL>>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _currencyId = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)tp)
                .ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);
        }

        public async Task ReSettle()
        {
            foreach (var currencyId in _tpCurrency)
            {
                var token = await _mgApi.GetToken(currencyId);
                if (string.IsNullOrEmpty(token)) return;
                var mgFailedTransactions = await _mgApi.GetFailedTransactionsAsync(token, currencyId);
                if (mgFailedTransactions != null && mgFailedTransactions.Count > 0)
                {
                    foreach (var transaction in mgFailedTransactions)
                    {
                        var request = new MGUpdatebalanceRequest()
                        {
                            txnType = transaction.actionType.ToUpper(),
                            txnEventType = transaction.txnEventType,
                            playerId = transaction.playerId,
                            amount = transaction.amount,
                            currency = currencyId,
                            txnId = transaction.txnId,
                            creationTime = DateTime.UtcNow.Ticks,
                            contentCode = transaction.contentCode,
                            betId = transaction.betId,
                            completed = transaction.completed,
                            channel = transaction.channel,
                            IsFailedTransaction = true
                        };

                        var isSuccess = await _swApi.MgUpdateBalance(request);
                        if (isSuccess)
                        {
                            _logger.LogInformation($"currency:{currencyId},transaction {JsonConvert.SerializeObject(request)}", "MG_ReSettle");
                            await _mgApi.UpdateFailedTransactionToResolvedState(token, transaction.txnId, currencyId);
                        }
                    }
                }
                Thread.Sleep(10000);
            }
        }

        public async Task<bool> GetReport(DateTime date)
        {
            foreach (var tpCurrencyId in _tpCurrency)
            {
                var token = await _mgApi.GetToken(tpCurrencyId);
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }


                var gameAnalyses = await _mgApi.GetPlayerGameAnalysisAsync(token, tpCurrencyId, date);
                if (gameAnalyses.Any())
                {
                    var stake = gameAnalyses.Sum(i => i.income);
                    var dailySummary = new List<FincanceDailySummary>() { new FincanceDailySummary(){
                        SummaryType = SummaryType.TpBetRecord,
                        SummaryDate = date,
                        TpId = (int)tp,
                        CurrencyId = _currencyId[tpCurrencyId],
                        Stake = stake,
                        ValidBet = stake,
                        Payout = gameAnalyses.Sum(i => i.payout),
                        WinlostAmount = gameAnalyses.Sum(i => i.grossWin),
                        ProgressiveBet = gameAnalyses.Sum(i => i.progressiveBetAmount),
                    }};
                    _fincanceService.SetFinanceTpDailySummary(dailySummary);
                }

                Thread.Sleep(10000);

                var fortuneRewards = await _mgApi.GetFortuneRewardsAsync(token, tpCurrencyId, date);
                if (fortuneRewards.Any())
                {
                    var fortun = GetRewardSummary(date, tpCurrencyId, fortuneRewards);
                    _fincanceService.SetFinanceTpDailySummary(fortun);
                }

                Thread.Sleep(10000);
            }
            return true;
        }

        private IEnumerable<FincanceDailySummary> GetRewardSummary(DateTime date, string tpCurrencyId, IEnumerable<MGFortuneRewards> fortuneRewards)
        {
            var currencyId = _currencyId[tpCurrencyId];
            var cash = new List<MGFortuneRewards>();
            var freeGame = new List<MGFortuneRewards>();

            foreach (var item in fortuneRewards)
            {
                if (int.TryParse(item.transactionId, out var _))
                {
                    freeGame.Add(item);
                }
                else
                {
                    cash.Add(item);
                }
            }

            return new List<FincanceDailySummary>()
            {
               new FincanceDailySummary()
               {
                    SummaryType = SummaryType.TpRewardTotalAmount,
                    SummaryDate = date,
                    TpId = (int)tp,
                    CurrencyId = currencyId,
                    Payout = fortuneRewards.Sum(i=>i.payout),
                    Reward = fortuneRewards.Sum(i=>i.rewardAmount),
               },
               new FincanceDailySummary()
               {
                    SummaryType = SummaryType.TpRewardCash,
                    SummaryDate = date,
                    TpId = (int)tp,
                    CurrencyId = currencyId,
                    Payout = cash.Sum(i=>i.payout),
                    Reward = cash.Sum(i=>i.rewardAmount),
               },
               new FincanceDailySummary()
               {
                    SummaryType = SummaryType.TpRewardFreeGame,
                    SummaryDate = date,
                    TpId = (int)tp,
                    CurrencyId = currencyId,
                    Payout = freeGame.Sum(i=>i.payout),
                    Reward = freeGame.Sum(i=>i.rewardAmount),
               }
            };
        }
    }


}