using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAMBBLL : ITpReportBLL
    {

        private readonly ILogger<FinanceAMBBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAMBBLL _apiAMBBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0; // 報表帶入參數為日期格式，對方API預設+8，後續功能有運用需將tpbetrecord轉換時間+8時間比對
        private readonly List<int> _gameTypeList = new() { (int)GameType.AMB_Poker, (int)GameType.AMB_RNG };
        private readonly int _tpId = (int)Tp.AMB;
        private readonly IEnumerable<TpCurrency> _tpCurrencyList;

        public FinanceAMBBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAMBBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAMBBLL = provider.GetRequiredService<ApiAMBBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _tpCurrencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var _currencyId = _tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsItemList_AMB>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.TpCurrencyId).Select(g => new
            {
                TpCurrencyId = g.Key,
                bet = g.Sum(s => s.bet),
                turnover = g.Sum(s => s.turnover),
                winlose = g.Sum(s => s.winlose),
                commission = g.Sum(s => s.commission),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.TpCurrencyId],
                    Stake = item.bet,
                    ValidBet = item.turnover,
                    Payout = item.bet + item.winlose,
                    WinlostAmount = item.winlose,
                    Revenue = item.commission,
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();

            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
            var serverList = _commonBLL.GetSiteList();

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);
                foreach (var item in _tpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var currency = item.TpCurrencyId;
                    foreach (var gameType in _gameTypeList)
                    {
                        // CNY、USD Poker未開放 暫時排除
                        if (gameType == (int)GameType.AMB_Poker && (currency == "USD" || currency == "CNY"))
                        {
                            continue;
                        }

                        // pagination
                        var page = 1;
                        int totalPage = 1;
                        var perPage = 500;
                        while (page <= totalPage)
                        {
                            Thread.Sleep(5000);
                            CurrencyStats_AMB getWagerSummary = await _apiAMBBLL.GetBetSummaryByTime(currency, gameType, reportTime.startTime, reportTime.endTime, perPage, page, site.SiteId);
                            if (getWagerSummary.status.code != 0)
                            {
                                var msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\n{gameType}\r\n{currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                                SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                                throw systemException;
                            }

                            if (getWagerSummary.status.code == 0 && getWagerSummary.data.response == null && getWagerSummary.data.pageNumber == 0)
                            {
                                var msg = "NO_DATA";
                                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                                continue;
                            }
                            totalPage = (getWagerSummary.data.totalRecords / perPage) + 1;

                            foreach (var dataList in getWagerSummary.data.response)
                            {
                                foreach (var data in dataList.result)
                                {
                                    data.TpCurrencyId = currency;
                                    reportData.Add(data);
                                }
                            }
                            page++;
                        }
                    }
                }
            }

            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
