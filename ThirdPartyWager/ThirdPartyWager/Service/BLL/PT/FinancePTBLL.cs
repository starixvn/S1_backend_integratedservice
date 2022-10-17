using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinancePTBLL : ITpReportBLL
    {
        private readonly ILogger<FinancePTBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiPTBLL _apiPTBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0; // 幣別時區不同，整天資料預設取0，後續功能有運用需將wager or tpbetrecord對應幣別轉換時間
        private readonly List<int> _siteList = new List<int>() { 1, 2, 3 };
        private readonly Dictionary<int, string> _siteCurrencyList = new Dictionary<int, string>() {
            {1, "MYR,USD,THB,CNY,IDR,INR"},
            {2, "VND"},
            {3, "VND"}
        };

        public FinancePTBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinancePTBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiPTBLL = provider.GetRequiredService<ApiPTBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var _currencyId = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_PT>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.CURRENCYCODE).Select(g => new
            {
                Currency = g.Key,
                BETS = g.Sum(s => s.BETS),
                WINS = g.Sum(s => s.WINS),
                JACKPOTBETS = g.Sum(s => s.JACKPOTBETS),
                JACKPOTWINS = g.Sum(s => s.JACKPOTWINS),
                INCOME = g.Sum(s => s.INCOME),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.Currency],
                    Stake = item.BETS,
                    ValidBet = item.BETS,
                    Payout = item.WINS,
                    WinlostAmount = item.WINS + item.JACKPOTWINS - item.BETS,
                    ProgressiveBet = item.JACKPOTBETS,
                    ProgressiveWin = item.JACKPOTWINS
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            foreach (var siteId in _siteList)
            {
                // 分Site取得TpList
                setting = _thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);

                // 各site執行幣別
                var getSiteCurrency = _siteCurrencyList[siteId];
                string[] siteCurrencyList = getSiteCurrency.Split(",");
                foreach (var currency in siteCurrencyList)
                {
                    var adminName = setting[$"AdminName_{currency}"];

                    var getWagerSummary = await _apiPTBLL.GetBetSummaryByTime(setting, currency, siteId, reportTime.startTime, reportTime.endTime);

                    if (getWagerSummary.errorcode != 0)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\nSiteId={siteId}\r\nCurrency={currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.pagination.totalCount == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nSiteId={siteId}\r\nCurrency={currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }

                    foreach (var item in getWagerSummary.result)
                    {
                        item.CURRENCYCODE = currency;
                        // prd entity
                        if (item.ENTITY == adminName)
                        {
                            reportData.Add(item);
                        }
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
