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
        private readonly IHostEnvironment _environment;
        private readonly int _timeZone = 8;

        public FinanceEbetBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceEbetBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiEbetBLL = provider.GetRequiredService<ApiEbetBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            foreach (var item in reportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
                    Stake = item.totalBet + item.totalWithholding,
                    ValidBet = item.totalBet,
                    Payout = item.totalPayout,
                    WinlostAmount = item.totalBalance
                });
            }
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            //setting無用
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                foreach (var item in tpCurrencyList)
                {
                    var currency = item.TpCurrencyId;

                    var getWagerSummary = await _apiEbetBLL.GetBetSummaryByTime(site.SiteId ,currency, reportTime.startTime, reportTime.endTime);
                    if (getWagerSummary.status != 200)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.count == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }
                    getWagerSummary.CurrencyId = item.CurrencyId;
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
