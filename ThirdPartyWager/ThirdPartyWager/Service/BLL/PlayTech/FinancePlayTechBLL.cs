using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinancePlayTechBLL : ITpReportBLL
    {
        private readonly ILogger<FinancePlayTechBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiPlayTechBLL _apiPlayTechBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerPlayTechService _wagerPlayTechService;
        private readonly int _timeZone = 0;

        public FinancePlayTechBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinancePlayTechBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiPlayTechBLL = provider.GetRequiredService<ApiPlayTechBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerPlayTechService = provider.GetRequiredService<WagerPlayTechService>();
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_PlayTech>>(JsonConvert.SerializeObject(reportData));

            foreach (var item in getReportData)
            {
                var f = new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.currency],
                    Stake = item.totalBets - item.totalBets,
                    ValidBet = item.totalBets - item.totalBets,
                    Payout = item.totalWins,
                    WinlostAmount = item.ggr,
                    ProgressiveBet = item.totalJpc,
                    ProgressiveWin = item.totalJpw
                };
                if (f.Stake != 0 && f.WinlostAmount != 0)
                {
                    dailyData.Add(f);
                }
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            foreach (var site in siteList)
            {
                setting = site.SettingDic;
                var getWagerSummary = await _apiPlayTechBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);
                if (getWagerSummary.error != "0")
                {
                    var msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\nSiteId=\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                    throw systemException;
                }

                if (getWagerSummary.error == "0" && getWagerSummary.data.Count == 0)
                {
                    var msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\nsiteId={site.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return reportData;
                }

                foreach (var item in getWagerSummary.data)
                {
                    reportData.Add(item);
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
