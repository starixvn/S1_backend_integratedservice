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
        private readonly List<string> _gametype = new List<string>() { "slot", "arcade", "fish", "table","CMCOCKFIGHT01"};
        private readonly List<string> _liveGameType = new List<string>() { "CMCOCKFIGHT01"};
        //後續有新的liveGame在_gametype及_liveGameType加入gameCode即可

        
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
            var data = WinloseSummary_CQ9(getReportData);
            
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
                    WinlostAmount = item.winlostAmount
                });
            }
            
            return dailyData;
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
            var reportData = new List<dynamic>();
            var getWagerSummary = new CurrencyStats_CQ9();

            foreach (var item in tpCurrencyList)
            {
                var currency = item.TpCurrencyId;
                var token = setting[$"API_Token_{currency}"];

                foreach (var gametype in _gametype)
                {
                    if (!_liveGameType.Contains(gametype))
                    {
                        getWagerSummary = await _apiCQ9BLL.GetBetSummaryByTime(setting, token, gametype, reportTime.startTime, reportTime.endTime);
                    }
                    else
                    {
                        getWagerSummary = await _apiCQ9BLL.GetLiveBetSummaryByTime(setting, token, gametype, reportTime.startTime, reportTime.endTime);
                    }
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
                    
                    foreach (var addType in getWagerSummary.data.data)
                    {
                        addType.gameType = gametype;
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

        private IEnumerable<dynamic> WinloseSummary_CQ9(List<CurrencyStatsData_CQ9> list)
        {
            var groupedData = list
                .GroupBy(i => new { i.gameType, i.currency })
                .Select(g => new
                {
                    gameType = g.Key.gameType,
                    currency = g.Key.currency,
                    bets = g.Sum(s => s.bets),
                    wins = g.Sum(s => s.wins),
                    validbet = g.Sum(s => s.validbet),
                    jackpots = g.Sum(s => s.jackpots),
                    rakes = g.Sum(s => s.rakes),
                    roomfee = g.Sum(s => s.roomfee),
                    winlostAmount = _liveGameType.Contains(g.Key.gameType) ? g.Sum(s => s.wins) : g.Sum(s => s.wins - s.bets)
                });
            
            var summaryByCurrency = groupedData
                .GroupBy(i => i.currency)
                .Select(g => new
                {
                    currency = g.Key,
                    bets = g.Sum(s => s.bets),
                    wins = g.Sum(s => s.wins),
                    validbet = g.Sum(s => s.validbet),
                    jackpots = g.Sum(s => s.jackpots),
                    rakes = g.Sum(s => s.rakes),
                    roomfee = g.Sum(s => s.roomfee),
                    winlostAmount = g.Sum(s => s.winlostAmount)
                });

            return summaryByCurrency;
        }
    }
}
