using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinanceEVOBLL : ITpReportBLL
    {
        private readonly ILogger<FinanceEVOBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiEVOBLL _apiEVOBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeZone = 0;

        public FinanceEVOBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceEVOBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiEVOBLL = provider.GetRequiredService<ApiEVOBLL>();
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

            var tmpReportData = new List<Participant_EVO>();
            var getReportData = JsonConvert.DeserializeObject<IList<WagerEVOModel>>(JsonConvert.SerializeObject(reportData[0]));
            foreach (var round in getReportData)
            {
                if (round.status != "Resolved")
                {
                    continue;
                }
                IList<Participant_EVO> participants = JsonConvert.DeserializeObject<IList<Participant_EVO>>(JsonConvert.SerializeObject(round.participants));
                foreach (var playerItem in participants)
                {
                    tmpReportData.Add(playerItem);
                }
            }

            if (tmpReportData.Any())
            {
                var data = tmpReportData.GroupBy(i => i.currency).Select(g => new
                {
                    Currency = g.Key,
                    stake = g.Sum(s => s.bets.Sum(p => p.stake)),
                    payout = g.Sum(s => s.bets.Sum(p => p.payout)),
                });

                foreach (var item in data)
                {
                    dailyData.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpBetRecord,
                        SummaryDate = reportDate,
                        TpId = tpId,
                        CurrencyId = _currencyId[item.Currency],
                        Stake = item.stake,
                        ValidBet = item.stake,
                        Payout = item.payout,
                        WinlostAmount = item.payout - item.stake
                    });
                }
            }

            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();

            List<WagerEVOModel> getWagerSummary = await _apiEVOBLL.GetBetListByTime(setting, reportTime.startTime, reportTime.endTime);

            var error = getWagerSummary.Select(x => x.error).FirstOrDefault();

            if (error != null && error != "nodata")
            {
                var msg = "THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                SystemException systemException = new($"{msg} - GetReport({tpId})");
                throw systemException;
            }

            if (error == "nodata")
            {
                var msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                return reportData;
            }

            reportData.Add(getWagerSummary);
            return reportData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
