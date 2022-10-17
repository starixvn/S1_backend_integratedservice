using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceCQ9BLL : ITpReportBLL
    {
        private readonly ILogger<FinanceCQ9BLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiCQ9BLL _apiCQ9BLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = -4;
        private readonly List<string> _gametype = new List<string>() { "slot", "arcade", "fish", "table" };

        public FinanceCQ9BLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceCQ9BLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiCQ9BLL = provider.GetRequiredService<ApiCQ9BLL>();
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

            var getReportData = JsonConvert.DeserializeObject<List<CurrencyStatsData_CQ9>>(JsonConvert.SerializeObject(reportData));
            var data = getReportData.GroupBy(i => i.currency).Select(g => new
            {
                currency = g.Key,
                bets = g.Sum(s => s.bets),
                wins = g.Sum(s => s.wins),
                validbet = g.Sum(s => s.validbet),
                jackpots = g.Sum(s => s.jackpots),
                rakes = g.Sum(s => s.rakes),
                roomfee = g.Sum(s => s.roomfee),
            });

            foreach (var item in data)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = _currencyId[item.currency],
                    Stake = item.bets,
                    ValidBet = item.bets,
                    Payout = item.wins,
                    WinlostAmount = item.wins - item.bets
                });
            }
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();

            foreach (var item in tpCurrencyList)
            {
                var currency = item.TpCurrencyId;
                var token = setting[$"API_Token_{currency}"];

                foreach (var gametype in _gametype)
                {
                    CurrencyStats_CQ9 getWagerSummary = await _apiCQ9BLL.GetBetSummaryByTime(setting, token, gametype, reportTime.startTime, reportTime.endTime);
                    if (getWagerSummary.status.code != "0")
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{currency}\r\n{gametype}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        SystemException systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    if (getWagerSummary.status.code == "0" && getWagerSummary.data.totalsize == 0)
                    {
                        var msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{currency}\r\n{gametype}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                        continue;
                    }

                    reportData.AddRange(getWagerSummary.data.data);
                }
            }

            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffzzz", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffzzz", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
