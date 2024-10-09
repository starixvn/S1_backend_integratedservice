using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceKMBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceKMBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly ApiKMBLL _apiKMBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly int _tpId = (int)Tp.KM;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;

        public FinanceKMBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceKMBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiKMBLL = provider.GetRequiredService<ApiKMBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _tpCurrencyList = _commonBLL.GetTpCurrencyList(_tpId);
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStats_KM>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.cur).Select(g => new
            {
                cur = g.Key,
                numberofbets = g.Sum(s => s.numberofbets),
                riskamt = g.Sum(s => s.riskamt),
                winamt = g.Sum(s => s.winamt),
                turnover = g.Sum(s => s.turnover),
                validbet = g.Sum(s => s.validbet),
                winloss = g.Sum(s => s.winloss),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.cur],
                    Stake = item.riskamt,
                    ValidBet = item.validbet,
                    Payout = item.winamt,
                    WinlostAmount = item.winloss,
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                foreach (var item in tpCurrencyList)
                {
                    var currency = item.TpCurrencyId;

                    var getWagerSummary = await _apiKMBLL.GetBetSummaryByTime(currency, reportTime.startTime, reportTime.endTime, site.SiteId);
                    // 我司将会限制 ** 历史注单 ** 相关的API请求频率 ( 允许商户每五秒请求一次 )
                    Thread.Sleep(8000);

                    if (getWagerSummary == null)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nCurrency={currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.Count() == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nCurrency={currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }

                    foreach (var data in getWagerSummary)
                    {
                        reportData.Add(data);
                    }
                }
            }
            
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            // 取整點結束時間
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime, 0, 0);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime.AddDays(-1), 0, 0);
            }
            return reportTime;
        }
    }
}
