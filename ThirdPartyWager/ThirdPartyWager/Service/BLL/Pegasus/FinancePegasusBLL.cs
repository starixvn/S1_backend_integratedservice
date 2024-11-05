using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class FinancePegasusBLL : ITpReportBLL
    {
        private readonly CommonBLL _commonBll;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ApiPegasusBLL _apiPegasusBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _timeZone = 0;
        private readonly List<TpCurrencyConfig> _currencyConfigList = new();
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;

        public FinancePegasusBLL(IServiceProvider provider)
        {
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _apiPegasusBll = provider.GetRequiredService<ApiPegasusBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(Tp.Pegasus.ToString());
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);

            foreach (var siteData in siteList)
            {
                if (siteData.TicketKey == 0)
                {
                    continue;
                }

                setting = _siteConfigDic[siteData.SiteId];
                PegasusConfig(setting);

                foreach (var currencyConfig in _currencyConfigList)
                {
                    var getWagerSummary = await _apiPegasusBll.GetBetSummaryByTime(setting["ApiUrl"], currencyConfig.SecretKey, currencyConfig.MerchantID, reportTime.startTime, reportTime.endTime);

                    if (getWagerSummary.Code != 0)
                    {
                        var msg = "THIRD_PARTY_ERROR";
                        var systemException = new SystemException($"{msg} - GetReport({tpId})");
                        throw systemException;
                    }

                    getWagerSummary.Currency = currencyConfig.Currency;
                    reportData.Add(getWagerSummary);
                }
            }

            return reportData;
        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            var dailyData = new List<FincanceDailySummary>();
            if (!reportData.Any())
            {
                return dailyData;
            }

            var tpCurrencyList = _commonBll.GetTpCurrencyList(tpId);
            var currencyId = tpCurrencyList.ToDictionary(i => i.TpCurrencyId, i => i.CurrencyId);

            var getReportData = JsonConvert.DeserializeObject<List<WagerData_Pegasus<WagerSummary_Pegasus>>>(JsonConvert.SerializeObject(reportData));

            foreach (var item in getReportData)
            {
                dailyData.Add(new FincanceDailySummary()
                {
                    SummaryType = SummaryType.TpBetRecord,
                    SummaryDate = reportDate,
                    TpId = tpId,
                    CurrencyId = currencyId[item.Currency],
                    Stake = item.Data.TotalBetAmount,
                    ValidBet = item.Data.TotalValidAmount,
                    Payout = item.Data.TotalPayoutAmount,
                    WinlostAmount = item.Data.TotalWinLossAmount
                });
            }

            return dailyData;
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            var reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = _dateTimeHelper.GetTimeByDay(1440, _timeZone, "yyyy-MM-dd HH:mm:ss", startTime.AddDays(-1));
            }
            return reportTime;
        }

        private void PegasusConfig(IReadOnlyDictionary<string, string> inputs)
        {
            foreach (var tpCurrencyId in _commonBll.GetTpCurrencyList((int)Tp.Pegasus))
            {
                if (inputs.ContainsKey($"Currency_{tpCurrencyId.TpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"Currency_{tpCurrencyId.TpCurrencyId}"]))
                {
                    var currencyConfig = JsonConvert.DeserializeObject<TpCurrencyConfig>(inputs[$"Currency_{tpCurrencyId.TpCurrencyId}"]);
                    if (currencyConfig is not null)
                    {
                        currencyConfig.Currency = tpCurrencyId.TpCurrencyId;
                        _currencyConfigList.Add(currencyConfig);
                    }
                }
            }
        }
    }
}
