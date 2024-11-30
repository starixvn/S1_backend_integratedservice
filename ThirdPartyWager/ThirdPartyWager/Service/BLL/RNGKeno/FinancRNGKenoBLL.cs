using System;
using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.PG;

public class FinancRNGKenoBLL : ITpReportBLL
{
    private readonly ILogger<FinancRNGKenoBLL> _logger;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly ApiMaxGameBLL _apiMaxGameBLL;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly int _timeZone = 0;
    private readonly Dictionary<int, RNGKenoConfig> _siteConfigDic;
    private readonly string _tpName = Tp.RNGKeno.ToString();
    
    public FinancRNGKenoBLL(IServiceProvider provider) {
        _logger = provider.GetRequiredService<ILogger<FinancRNGKenoBLL>>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetService<CommonBLL>();
        _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
        _dateTimeHelper = provider.GetService<DateTimeHelper>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<RNGKenoConfig>(_tpName);
    }

    public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
    {
        var dailyData = new List<FincanceDailySummary>();

        var tpCurrencyList = _commonBLL.GetTpCurrencyList(tpId);
        var currencyIdDic = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);


        var getReportData = 
            JsonConvert.DeserializeObject<List<PayloadDailySummaryData_MaxGame>>(
                JsonConvert.SerializeObject(reportData));

        var flattenedData = getReportData
            .Select(i => i.GameTypes)
            .SelectMany(x => x)
            .Select(i => i.BettingDate)
            .SelectMany(x => x)
            .ToArray();

        return flattenedData
            .GroupBy(i => i.CurrencyIso)
            .Select(g => new {
                CurrencyId = currencyIdDic.GetValue(g.Key),
                Stake = g.Sum(s => s.Stake),
                ActualStake = g.Sum(s => s.ActualStake),
                NetStake = g.Sum(s => s.NetStake),
                WinLose = g.Sum(s => s.WinLose),
                JackpotContributionBetCount = g.Sum(s => s.JackpotContributionBetCount),
                JackpotContribution = g.Sum(s => s.JackpotContribution),
                JackpotPrizeBetCount = g.Sum(s => s.JackpotPrizeBetCount),
                JackpotPrize = g.Sum(s => s.JackpotPrize),
                PromotionPrizeBetCount = g.Sum(s => s.PromotionPrizeBetCount),
                PromotionPrize = g.Sum(s => s.PromotionPrize)
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

        foreach (var site in siteList) {
            var config = _siteConfigDic[site.SiteId];
            
            var dailyBetSummary = 
                await _apiMaxGameBLL.GetDailySummaryDataAsync(
                    config,
                    _tpName,
                    new PayloadRequestDailySummaryData_MaxGame() {
                        TargetDate = GetCheckDateFromUnixTimestamp(reportTime.startUnix),
                        GameType = config.GameType
                    }
                );

            if (dailyBetSummary.ErrorCode != 0)  { 
                var msg = $"Bet result check API Error {dailyBetSummary.ErrorCode}";
                _logger.LogError($"{msg}{Environment.NewLine}{JsonConvert.SerializeObject(dailyBetSummary)}");
             
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

    private string GetCheckDateFromUnixTimestamp(long unixTimestamp) {
        var dateTime = DateTimeOffset
            .FromUnixTimeSeconds(unixTimestamp)
            .DateTime;
        
        var fortmattedDate = dateTime.ToString("yyyy-MM-dd");
        return fortmattedDate;
    }
}
