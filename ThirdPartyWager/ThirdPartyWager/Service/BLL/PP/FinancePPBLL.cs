using System;
using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Helper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.PP;

public class FinancePPBLL: ITpReportBLL
{
    private readonly ILogger<FinancePPBLL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ApiMaxGameBLL _apiMaxGameBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly int _timeZone = 0;
    private readonly Dictionary<int, PPConfig> _siteConfigDic;
    private readonly string _tpName = Tp.PP.ToString();

    public FinancePPBLL(IServiceProvider provider) {
        _logger = provider.GetRequiredService<ILogger<FinancePPBLL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
        _dateTimeHelper = provider.GetService<DateTimeHelper>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PPConfig>(_tpName);
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

    public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
    {
        var reportData = new List<dynamic>();
        var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);

        foreach (var site in siteList)
        {
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

            var dailyBetSummaryForLiveCasino =
                await _apiMaxGameBLL.GetDailySummaryDataAsync(
                    config,
                    _tpName,
                    new PayloadRequestDailySummaryData_MaxGame()
                    {
                        TargetDate = GetCheckDateFromUnixTimestamp(reportTime.startUnix),
                        GameType = config.LiveCasinoGameType
                    }
                );

            if (dailyBetSummary.ErrorCode != 0)
            {
                _logger.LogError("{DaiLySummaryPPRngError}",
                    JsonConvert.SerializeObject(dailyBetSummary)
                );
            }

            if (dailyBetSummaryForLiveCasino.ErrorCode != 0)
            {
                _logger.LogError("{DaiLySummaryPPLiveCasinoError}",
                    JsonConvert.SerializeObject(dailyBetSummary)
                );
            }

            if (dailyBetSummaryForLiveCasino.ErrorCode != 0 && dailyBetSummary.ErrorCode != 0) 
                continue;

            if (dailyBetSummaryForLiveCasino.Payload.GameTypes.Any())
            {
                reportData.Add(dailyBetSummaryForLiveCasino.Payload);
            }
            
            if (dailyBetSummary.Payload.GameTypes.Any())
            {
                reportData.Add(dailyBetSummary.Payload);
            }
        }

        return reportData;
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

    private string GetCheckDateFromUnixTimestamp(long unixTimestamp)
    {
        var dateTime = DateTimeOffset
            .FromUnixTimeSeconds(unixTimestamp)
            .DateTime;

        var fortmattedDate = dateTime.ToString("yyyy-MM-dd");
        return fortmattedDate;
    }
}
