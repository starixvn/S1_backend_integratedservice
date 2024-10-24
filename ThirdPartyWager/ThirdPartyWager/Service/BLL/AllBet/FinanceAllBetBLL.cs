using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceAllBetBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceAllBetBLL> _logger;
        private readonly CommonBLL _commonBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 8;
        private readonly Dictionary<int, AllBetConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.Allbet;

        public FinanceAllBetBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceAllBetBLL>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(Tp.Allbet.ToString());
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_AllBet>>(JsonConvert.SerializeObject(reportData));
            var groupData = getReportData.GroupBy(i => i.CurrencyId).Select(g => new
            {
                CurrencyId = g.Key,
                totalBetAmount = g.Sum(s => s.totalBetAmount),
                totalWinOrLoss = g.Sum(s => s.totalWinOrLoss),
            });

            foreach (var item in groupData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = item.CurrencyId,
                    Stake = item.totalBetAmount,
                    ValidBet = item.totalBetAmount,
                    Payout = item.totalBetAmount + item.totalWinOrLoss,
                    WinlostAmount = item.totalWinOrLoss
                });
            }

            return dailyData;
        }

        public Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            //setting 無用
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();
            var serverList = _commonBLL.GetSiteList();
            var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }

                var _siteAllBetConfig = _siteConfigDic[site.SiteId];
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);

                foreach (var item in tpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var currency = item.TpCurrencyId;
                    var agent = _siteAllBetConfig.CurrencyAgents.FirstOrDefault(x => x.CurrencyId == currency)?.Agent;
                    var getWagerSummary = _apiAllBetBLL.GetBetSummaryByTime(agent, reportTime.startTime, site.SiteId);
                    Thread.Sleep(150000);

                    if (getWagerSummary == null)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    var errorCode = getWagerSummary.resultCode;
                    if (errorCode != "OK")
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.data.count == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\nagent={agent}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }

                    getWagerSummary.data.CurrencyId = item.CurrencyId;
                    reportData.Add(getWagerSummary.data);
                }
            }

            return Task.FromResult(reportData);
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
