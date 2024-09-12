using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class FinanceMP : BackgroundService
    {
        private readonly ILogger<FinanceMP> _logger;
        private readonly FinanceMPBLL _financeMPBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly FincanceService _fincanceService;
        private readonly int _tpId = (int)Tp.MP;
        private readonly string _tpName = Tp.MP.ToString();
        private readonly int _tpType = (int)WagerTimerType.Finance;
        private readonly int _beyond = 2;
        private Timer timer;
        private readonly Dictionary<int, MPConfig> _siteConfigDic;

        public FinanceMP(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FinanceMP>>();
            _financeMPBLL = provider.GetRequiredService<FinanceMPBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _fincanceService = provider.GetRequiredService<FincanceService>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MPConfig>(Tp.MP.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoReport, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoReport(object state)
        {
            var quick = 0;
            try
            {
                LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
                LogContext.PushProperty("Tp", _tpName);
                var res = await ExecReportAsync();
                if (res)
                {
                    quick = 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                if (quick == 1)
                {
                    var dueTimeSpan = TimeSpan.FromHours(1);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromHours(4);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }


        private async Task<bool> ExecReportAsync()
        {
            var quick = false;
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var siteData in siteList)
                {
                    if (_siteConfigDic.TryGetValue(siteData.SiteId, out var _setting))
                    {
                        // get timer
                        var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                        // default time
                        var execTime = new ThirdPartyTimerModel()
                        {
                            TpId = _tpId,
                            Type = _tpType,
                            StartTime = (dataTpTimer == null) ? DateTime.UtcNow.AddDays(-2) : dataTpTimer.StartTime.AddSeconds(1),
                        };

                        // get time 
                        var day = 0;
                        TimeCast reportTime = _financeMPBLL.GetReportTime(execTime.StartTime, day);

                        // report
                        DateTime reportDate = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(reportTime.endUnix);
                        var reportData = await _financeMPBLL.GetReport(_tpId, _setting, reportTime);
                        int result;
                        if (reportData.Count > 0)
                        {
                            // process
                            var dailyData = _financeMPBLL.DealReport(_tpId, reportDate, reportData);

                            // write
                            result = _fincanceService.SetFinanceTpDailySummary(dailyData);
                        }
                        else
                        {
                            result = 1;
                        }

                        // update timer
                        if (result > 0)
                        {
                            _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, reportTime.endUnix);
                        }

                        var diffDay = (int)(DateTime.Now.Date - reportDate.Date).TotalDays;
                        if (diffDay > _beyond)
                        {
                            quick = true;
                        }

                        _logger.LogInformation($"{_tpName} Get {reportDate:yyyy-MM-dd} report");
                    }
                }              
                return quick;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                quick = true;
            }

            _logger.LogInformation($"Report Worker Execute End {_tpId}");
            return quick;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
