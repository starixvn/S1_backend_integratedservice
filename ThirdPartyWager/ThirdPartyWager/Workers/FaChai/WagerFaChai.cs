using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerFaChai : BackgroundService
    {
        private readonly int _tpId = (int)Tp.FaChai;
        private readonly string _tpName = Tp.FaChai.ToString();
        private Timer _timer;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerFaChaiBLL _wagerFaChaiBLL;
        private readonly CommonBLL _commonBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<WagerFaChai> _logger;
        private readonly int timeZone = -4;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerFaChai(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>((Tp.FaChai).ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerFaChaiBLL = provider.GetRequiredService<WagerFaChaiBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _logger = provider.GetRequiredService<ILogger<WagerFaChai>>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                var TpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.FaChai).Select(x => x.TpCurrencyId).Distinct();
                var time = _dateTimeHelper.GetTimeByMinuteRevert(iRangMins, timeZone, _timeFormat, DateTime.UtcNow);
                foreach (var siteData in siteList)
                {
                    if (siteData.TicketKey == 0)
                    {
                        _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                        continue;
                    }

                    foreach (var currency in TpCurrencyList)
                    {
                        await _wagerFaChaiBLL.GetWagerAsync(_siteConfigDic[siteData.SiteId], time.startTime, time.endTime, currency);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
