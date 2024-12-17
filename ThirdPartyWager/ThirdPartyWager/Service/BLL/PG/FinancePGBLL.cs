using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Helper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.PG;

public class FinancePGBLL : ITpReportBLL
{
    private readonly ILogger<FinancePGBLL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ApiMaxGameBLL _apiMaxGameBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly int _timeZone = 0;
    private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;
    private readonly string _tpName = Tp.PG.ToString();

    public FinancePGBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<FinancePGBLL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetService<CommonBLL>();
        _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
        _dateTimeHelper = provider.GetService<DateTimeHelper>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
    }

    public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
    {
        var currencyIdDic = _commonBLL
            .GetTpCurrencyList(tpId)
            .ToDictionary(i => i.TpCurrencyId,
                i => new CurrencyModel
                {
                    CurrencyId = i.CurrencyId,
                    ExchangeRate = i.ExchangeRate
                });

        return TpHelper.DealReport(currencyIdDic, tpId, reportDate, reportData);
    }

    public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
    {
        var reportData = new List<dynamic>();
        var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);

        foreach (var site in siteList)
        {
            var config = _siteConfigDic[site.SiteId];
            if (string.IsNullOrWhiteSpace(config.DailySumApiUrl) || _siteConfigDic[site.SiteId] is null)
            {
                _logger.LogWarning($"GetDailySummary of {config.GameType} is ignore because {config.DailySumApiUrl} is empty");
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
            
            if (!dailyBetSummary.Payload.GameTypes.Any()) {
                _logger.LogWarning($"GetDailySummary of {config.GameType} return no data");
                continue;
            }

            if (dailyBetSummary.Payload.GameTypes.Any())
            {
                reportData.Add(dailyBetSummary.Payload);
            }
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

    private string GetCheckDateFromUnixTimestamp(long unixTimestamp)
    {
        var dateTime = DateTimeOffset
            .FromUnixTimeSeconds(unixTimestamp)
            .DateTime;

        var fortmattedDate = dateTime.ToString("yyyy-MM-dd");
        return fortmattedDate;
    }
}