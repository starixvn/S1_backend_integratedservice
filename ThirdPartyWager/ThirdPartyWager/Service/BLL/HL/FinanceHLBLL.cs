using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.HL
{
    public class FinanceHLBLL :ITpReportBLL
    {
        private readonly ILogger<FinanceHLBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly ApiHLBLL _apiHLBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly string _tpName = Tp.HaoLong.ToString();
        private readonly HLConfig _siteHLConfig;
        private readonly int _timeZone = -4;

        public FinanceHLBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceHLBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _apiHLBLL = provider.GetRequiredService<ApiHLBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(_tpName);
        }

        public TimeCast GetReportTime(DateTime startTime, int day)
        {
            throw new NotImplementedException();
        }

        public async Task<List<dynamic>> GetReport(int tpId, dynamic setting, TimeCast reportTime)
        {
            var reportData = new List<dynamic>();
            
            var tasks = new List<Task<IEnumerable<FincanceDailySummary>>>
            {
                _commonBLL.GetWagerSummary("Finance_TpWager_HL_Summary_Get", reportTime.startUnix, reportTime.endUnix),
            };

            await Task.WhenAll(tasks);

            foreach (var item in tasks)
            {
                reportData.AddRange(item.Result);
            }
            return reportData;

        }

        public IEnumerable<FincanceDailySummary> DealReport(int tpId, DateTime reportDate, List<dynamic> reportData)
        {
            throw new NotImplementedException();
        }
    }
}