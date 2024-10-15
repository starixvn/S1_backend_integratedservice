using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceEbetBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceEbetBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly ApiEbetBLL _apiEbetBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly int _timeZone = 8;
        private readonly Tp _tp = Tp.EBet;

        public FinanceEbetBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceEbetBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiEbetBLL = provider.GetRequiredService<ApiEbetBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList((int)_tp);
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var tpCurrencyId = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);
            
            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStats_Ebet>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.CurrencyId).Select(g => new
            {
                Currency = g.Key,
                TotalBet = g.Sum(s => s.totalBet),
                TotalWithholding = g.Sum(s => s.totalWithholding),
                TotalPayout = g.Sum(s => s.totalPayout),
                TotalBalance = g.Sum(s => s.totalBalance),
            });
            
            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = tpCurrencyId[item.Currency.ToString()],
                    Stake = item.TotalBet + item.TotalWithholding,
                    ValidBet = item.TotalBet,
                    Payout = item.TotalPayout,
                    WinlostAmount = item.TotalBalance
                });
            }
            
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            //setting無用
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            var serverList = _commonBLL.GetSiteList();
            var reportData = new List<dynamic>();

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }
                
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);
                foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)))
                    
                {
                    var getWagerSummary = await _apiEbetBLL.GetBetSummaryByTime(site.SiteId ,currency.TpCurrencyId, reportTime.startTime, reportTime.endTime);
                    if (getWagerSummary.status != 200)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency.TpCurrencyId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.count == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }
                    getWagerSummary.CurrencyId = currency.CurrencyId;
                    reportData.Add(getWagerSummary);
                }
            }
            
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
