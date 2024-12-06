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
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly FinanceService _financeService;
        private readonly Tp _tp = Tp.MG;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly Dictionary<string, int> _currencyId;


        public MGBLL(IServiceProvider provider)
        {
            _mgApi = provider.GetRequiredService<IApiMGBLL>();
            _swApi = provider.GetRequiredService<IApiSWBLL>();
            _logger = provider.GetRequiredService<ILogger<MGBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _financeService = provider.GetRequiredService<FinanceService>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)_tp);
            _currencyId = _tpCurrency.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);
        }

        public async Task ReSettle()
        {
            var siteList = _thirdPartyBLL.GetTpListByTpId((int)_tp);
            var serverList = _commonBLL.GetSiteList();
            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);
                foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var token = await _mgApi.GetToken(currency.TpCurrencyId, site.SiteId);
                    if (string.IsNullOrEmpty(token)) return;
                    var mgFailedTransactions = await _mgApi.GetFailedTransactionsAsync(token, currency.TpCurrencyId, site.SiteId);
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
                                _logger.LogInformation($"siteId:{site.SiteId},currency:{currency.TpCurrencyId},transaction {JsonConvert.SerializeObject(request)}", "MG_ReSettle");
                                await _mgApi.UpdateFailedTransactionToResolvedState(token, transaction.txnId, currency.TpCurrencyId, site.SiteId);
                            }
                        }
                    }
                    Thread.Sleep(10000);
                }
            }
        }

        public async Task<bool> GetReport(DateTime date)
        {
            var siteList = _thirdPartyBLL.GetTpListByTpId((int)_tp);
            var serverList = _commonBLL.GetSiteList();
            
            var gameAnalysesDailySummary = new List<FincanceDailySummary>();
            var tpRewardTotalAmountDailySummary = new List<FincanceDailySummary>();
            var tpRewardCashDailySummary = new List<FincanceDailySummary>();
            var tpRewardFreeGameDailySummary = new List<FincanceDailySummary>();

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }
                
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);
                foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var token = await _mgApi.GetToken(currency.TpCurrencyId, site.SiteId);
                    if (string.IsNullOrEmpty(token))
                    {
                        return false;
                    }

                    var gameAnalyses = await _mgApi.GetPlayerGameAnalysisAsync(token, currency.TpCurrencyId, site.SiteId, date);
                    if (gameAnalyses.Any())
                    {
                        var stake = gameAnalyses.Sum(i => i.income);
                        gameAnalysesDailySummary.Add(new FincanceDailySummary()
                        {
                            CurrencyId = _currencyId[currency.TpCurrencyId],
                            Stake = stake,
                            ValidBet = stake,
                            Payout = gameAnalyses.Sum(i => i.payout),
                            WinlostAmount = gameAnalyses.Sum(i => i.grossWin),
                            ProgressiveBet = gameAnalyses.Sum(i => i.progressiveBetAmount),
                        });
                    }
                    Thread.Sleep(10000);

                    var fortuneRewards = await _mgApi.GetFortuneRewardsAsync(token, currency.TpCurrencyId, site.SiteId, date);
                    if (fortuneRewards.Any())
                    {
                        var fortun = GetRewardSummary(date, currency.TpCurrencyId, fortuneRewards);
                        tpRewardTotalAmountDailySummary.Add(fortun.First(i => i.SummaryType == SummaryType.TpRewardTotalAmount));
                        tpRewardCashDailySummary.Add(fortun.First(i => i.SummaryType == SummaryType.TpRewardCash));
                        tpRewardFreeGameDailySummary.Add(fortun.First(i => i.SummaryType == SummaryType.TpRewardFreeGame));
                    }
                    Thread.Sleep(10000);
                }
            }

            if (gameAnalysesDailySummary.Any())
            {
                var data = gameAnalysesDailySummary.GroupBy(i => i.CurrencyId).Select(g => new
                {
                    CurrencyId = g.Key,
                    Stake = g.Sum(s => s.Stake),
                    ValidBet = g.Sum(s => s.ValidBet),
                    Payout = g.Sum(s => s.Payout),
                    WinlostAmount = g.Sum(s => s.WinlostAmount),
                    ProgressiveBet = g.Sum(s => s.ProgressiveBet),
                });

                var dailyData = new List<FincanceDailySummary>();
                foreach (var item in data)
                {
                    dailyData.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpBetRecord,
                        SummaryDate = date,
                        TpId = (int)_tp,
                        CurrencyId = item.CurrencyId,
                        Stake = item.Stake,
                        ValidBet = item.ValidBet,
                        Payout = item.Payout,
                        WinlostAmount = item.WinlostAmount
                    });
                }
                _financeService.SetFinanceTpDailySummary(dailyData);
            }

            if (tpRewardTotalAmountDailySummary.Any())
            {
                var data = tpRewardTotalAmountDailySummary.GroupBy(i => i.CurrencyId).Select(g => new
                {
                    CurrencyId = g.Key,
                    Payout = g.Sum(s => s.Payout),
                    Reward = g.Sum(s => s.Reward),
                });

                var dailyData = new List<FincanceDailySummary>();
                foreach (var item in data)
                {
                    dailyData.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpRewardTotalAmount,
                        SummaryDate = date,
                        TpId = (int)_tp,
                        CurrencyId = item.CurrencyId,
                        Payout = item.Payout,
                        Reward = item.Reward
                    });
                }
                _financeService.SetFinanceTpDailySummary(dailyData);
            }

            if (tpRewardCashDailySummary.Any())
            {
                var data = tpRewardCashDailySummary.GroupBy(i => i.CurrencyId).Select(g => new
                {
                    CurrencyId = g.Key,
                    Payout = g.Sum(s => s.Payout),
                    Reward = g.Sum(s => s.Reward),
                });

                var dailyData = new List<FincanceDailySummary>();
                foreach (var item in data)
                {
                    dailyData.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpRewardCash,
                        SummaryDate = date,
                        TpId = (int)_tp,
                        CurrencyId = item.CurrencyId,
                        Payout = item.Payout,
                        Reward = item.Reward
                    });
                }
                _financeService.SetFinanceTpDailySummary(dailyData);
            }

            if (tpRewardFreeGameDailySummary.Any())
            {
                var data = tpRewardFreeGameDailySummary.GroupBy(i => i.CurrencyId).Select(g => new
                {
                    CurrencyId = g.Key,
                    Payout = g.Sum(s => s.Payout),
                    Reward = g.Sum(s => s.Reward),
                });

                var dailyData = new List<FincanceDailySummary>();
                foreach (var item in data)
                {
                    dailyData.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpRewardFreeGame,
                        SummaryDate = date,
                        TpId = (int)_tp,
                        CurrencyId = item.CurrencyId,
                        Payout = item.Payout,
                        Reward = item.Reward
                    });
                }
                _financeService.SetFinanceTpDailySummary(dailyData);
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