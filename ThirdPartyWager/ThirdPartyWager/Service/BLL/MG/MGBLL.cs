using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class MGBLL : IMGBLL
    {
        private readonly IApiMGBLL _mgApi;
        private readonly IApiSWBLL _swApi;
        private readonly ILogger<MGBLL> _logger;
        private readonly FincanceService _fincanceService;
        private readonly Tp _tp = Tp.MG;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly Dictionary<string, int> _currencyId;

        public MGBLL(IServiceProvider provider)
        {
            _mgApi = provider.GetRequiredService<IApiMGBLL>();
            _swApi = provider.GetRequiredService<IApiSWBLL>();
            _logger = provider.GetRequiredService<ILogger<MGBLL>>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)_tp);
            _currencyId = _tpCurrency.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);
        }

        public async Task ReSettle()
        {
            foreach (var currency in _tpCurrency)
            {
                var token = await _mgApi.GetToken(currency.TpCurrencyId);
                if (string.IsNullOrEmpty(token)) return;
                var mgFailedTransactions = await _mgApi.GetFailedTransactionsAsync(token, currency.TpCurrencyId);
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
                            currency = currency.TpCurrencyId,
                            txnId = transaction.txnId,
                            creationTime = DateTime.UtcNow.Ticks,
                            creationTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            contentCode = transaction.contentCode,
                            betId = transaction.betId,
                            completed = transaction.completed,
                            channel = transaction.channel,
                            IsFailedTransaction = true
                        };

                        var isSuccess = await _swApi.MgUpdateBalance(request);
                        if (isSuccess)
                        {
                            _logger.LogInformation($"currency:{currency.TpCurrencyId},transaction {JsonConvert.SerializeObject(request)}", "MG_ReSettle");
                            await _mgApi.UpdateFailedTransactionToResolvedState(token, transaction.txnId, currency.TpCurrencyId);
                        }
                    }
                }
                Thread.Sleep(10000);
            }
        }

        public async Task<bool> GetReport(DateTime date)
        {
            foreach (var currency in _tpCurrency)
            {
                var token = await _mgApi.GetToken(currency.TpCurrencyId);
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var gameAnalyses = await _mgApi.GetPlayerGameAnalysisAsync(token, currency.TpCurrencyId, date);
                if (gameAnalyses.Any())
                {
                    var stake = gameAnalyses.Sum(i => i.income);
                    var dailySummary = new List<FincanceDailySummary>() { new FincanceDailySummary(){
                        SummaryType = SummaryType.TpBetRecord,
                        SummaryDate = date,
                        TpId = (int)_tp,
                        CurrencyId = _currencyId[currency.TpCurrencyId],
                        Stake = stake,
                        ValidBet = stake,
                        Payout = gameAnalyses.Sum(i => i.payout),
                        WinlostAmount = gameAnalyses.Sum(i => i.grossWin),
                        ProgressiveBet = gameAnalyses.Sum(i => i.progressiveBetAmount),
                    }};
                    _fincanceService.SetFinanceTpDailySummary(dailySummary);
                }

                Thread.Sleep(10000);

                var fortuneRewards = await _mgApi.GetFortuneRewardsAsync(token, currency.TpCurrencyId, date);
                if (fortuneRewards.Any())
                {
                    var fortun = GetRewardSummary(date, currency.TpCurrencyId, fortuneRewards);
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
                    TpId = (int)_tp,
                    CurrencyId = currencyId,
                    Payout = fortuneRewards.Sum(i=>i.payout),
                    Reward = fortuneRewards.Sum(i=>i.rewardAmount),
               },
               new FincanceDailySummary()
               {
                    SummaryType = SummaryType.TpRewardCash,
                    SummaryDate = date,
                    TpId = (int)_tp,
                    CurrencyId = currencyId,
                    Payout = cash.Sum(i=>i.payout),
                    Reward = cash.Sum(i=>i.rewardAmount),
               },
               new FincanceDailySummary()
               {
                    SummaryType = SummaryType.TpRewardFreeGame,
                    SummaryDate = date,
                    TpId = (int)_tp,
                    CurrencyId = currencyId,
                    Payout = freeGame.Sum(i=>i.payout),
                    Reward = freeGame.Sum(i=>i.rewardAmount),
               }
            };
        }
    }

}