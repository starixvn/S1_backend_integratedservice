using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAPBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceAPBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAPBLL _apiAPBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;
        private readonly Dictionary<int, APConfig> _siteConfigDic;

        public FinanceAPBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAPBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAPBLL = provider.GetRequiredService<ApiAPBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<APConfig>(Tp.AP.ToString());
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_AP>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.Currency).Select(g => new
            {
                Currency = g.Key,
                TotalStake = g.Sum(s => s.TotalStake),
                TotalWin = g.Sum(s => s.TotalWin),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.Currency],
                    Stake = item.TotalStake,
                    ValidBet = item.TotalStake,
                    Payout = item.TotalWin,
                    WinlostAmount = item.TotalWin - item.TotalStake
                });
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
            foreach (var siteData in siteList)
            {
                if (siteData.TicketKey == 0)
                {
                    _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                    continue;
                }

                // 分Site取得TpList
                setting = _siteConfigDic[siteData.SiteId];

                foreach (var type in new List<string> { "slot", "mini" })
                {
                    switch (type)
                    {
                        case "slot":
                            var getWagerSummary = await _apiAPBLL.GetBetSummaryByTime(setting, reportTime.startTime, reportTime.endTime);
                            if (getWagerSummary.ErrorCode != 0)
                            {
                                var msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                                SystemException systemException = new($"{msg} - GetReport({tpId})");
                                throw systemException;
                            }

                            if (getWagerSummary.ErrorCode == 0 && getWagerSummary.Results == null)
                            {
                                var msg = "NO_DATA";
                                _logger.LogInformation($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                                continue;
                            }

                            foreach (var item in getWagerSummary.Results)
                            {
                                reportData.Add(item);
                            }
                            break;
                        case "mini":
                            var getWagerSummaryOfMini = await _apiAPBLL.GetBetListByTimeOfMini(setting, reportTime.startTime, reportTime.endTime);
                            if (getWagerSummaryOfMini == null || getWagerSummaryOfMini.httpStatus != 0)
                            {
                                var msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummaryOfMini)}");
                                SystemException systemException = new($"{msg} - GetReport({tpId})");
                                throw systemException;
                            }

                            if (getWagerSummaryOfMini.httpStatus == 0 && getWagerSummaryOfMini.result.Count == 0)
                            {
                                var msg = "NO_DATA";
                                _logger.LogInformation($"{msg}\r\nSiteId={siteData.SiteId}\r\n{JsonConvert.SerializeObject(getWagerSummaryOfMini)}");
                                continue;
                            }

                            foreach (var item in getWagerSummaryOfMini.result)
                            {
                                reportData.Add(item);
                            }
                            break;
                        default:
                            break;
                    }
                }

            }
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            // 取整點結束時間
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", startTime, 0, 0);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", startTime.AddDays(-1), 0, 0);
            }
            return reportTime;
        }
    }
}
