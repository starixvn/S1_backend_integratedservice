using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class FinanceTableGameBLL : ITpReportBLL
{
    private readonly ILogger<FinanceTableGameBLL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ApiMaxGameBLL _apiMaxGameBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly int _timeZone = 0;
    private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;
    private readonly string _tpName = Tp.TableGame.ToString();

    public FinanceTableGameBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinanceTableGameBLL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetService<CommonBLL>();
        _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
        _dateTimeHelper = provider.GetService<DateTimeHelper>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
    }

    public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
    {
        var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
        var currencyIdDic = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

        var getReportData =
            JsonConvert.DeserializeObject<List<PayloadDailySummaryData_MaxGame>>(
                JsonConvert.SerializeObject(reportData));

        return getReportData
            .Select(i => i.GameTypes)
            .SelectMany(x => x)
            .Select(i => i.BettingDate)
            .SelectMany(x => x)
            .GroupBy(i => i.CurrencyIso)
            .Select(g => new
            {
                CurrencyId = currencyIdDic.GetValue(g.Key),
                Stake = g.Sum(s => s.Stake),
                ActualStake = g.Sum(s => s.ActualStake),
                NetStake = g.Sum(s => s.NetStake),
                WinLose = g.Sum(s => s.WinLose)
            }).Select(item => new FincanceDailySummary
            {
                SummaryType = SummaryType.TpBetRecord,
                SummaryDate = reportDate,
                TpId = tpId,
                CurrencyId = item.CurrencyId,
                Stake = item.Stake,
                ValidBet = item.Stake,
                WinlostAmount = item.WinLose - item.NetStake,
                Payout = item.WinLose
            });
    }

    public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
    {
        var reportData = new List<dynamic>();
        var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);

        foreach (var site in siteList)
        {
            if (!_siteConfigDic.ContainsKey(site.SiteId))
            {
                continue;
            }
            var config = _siteConfigDic[site.SiteId];
            if (string.IsNullOrWhiteSpace(config.DailySumApiUrl))
            {
                _logger.LogWarning("GetDailySummary of {GameType} is ignore because {DailySumApiUrl} is empty", config.GameType, config.DailySumApiUrl);
                continue;
            }
            var dailyBetSummary =
                await _apiMaxGameBLL.GetDailySummaryDataAsync(
                    config,
                    _tpName,
                    new PayloadRequestDailySummaryData_MaxGame()
                    {
                        TargetDate = GetCheckDateFromUnixTimestamp(reportTime.startUnix),
                        GameType = config.GameType
                    }
                );

            if (dailyBetSummary.ErrorCode != 0)
            {
                _logger.LogError("{DaiLySummaryTableGameError}",
                    JsonConvert.SerializeObject(dailyBetSummary)
                    );

                continue;
            }

            if (!dailyBetSummary.Payload.GameTypes.Any()) {
                _logger.LogWarning("GetDailySummary of {GameType} return no data", config.GameType);
                continue;
            }

            reportData.Add(dailyBetSummary.Payload);
        }

        return reportData;
    }

    public TimeCast GetReportTime(DateTime startTime, int day)
    {
        // copy from FinanceSexyBLL, please review
        var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime);
        var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
        if (reportTime.endUnix > cvTime)
        {
            reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd'T'HH:mm:ss.fffZ", startTime.AddDays(-1));
        }
        return reportTime;
    }

    private static string GetCheckDateFromUnixTimestamp(long unixTimestamp)
    {
        var dateTime = DateTimeOffset
            .FromUnixTimeSeconds(unixTimestamp)
            .DateTime;

        var formattedDate = dateTime.ToString("yyyy-MM-dd");
        return formattedDate;
    }
}