using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerWonCasino : BackgroundService
    {
        private readonly int _tpId = (int)Tp.WonCasino;
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly string _tpName = Tp.WonCasino.ToString();
        private Timer _timer;
        private readonly Dictionary<int, WonCasinoConfig> _siteConfigDic;
        private readonly WagerWonCasinoBLL _wagerWonCasinoBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerWonCasino> _logger;       
        private readonly int timeZone = 0;
        private readonly int iRangMins = 5;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerWonCasino(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerWonCasino>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerWonCasinoBLL = provider.GetRequiredService<WagerWonCasinoBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WonCasinoConfig>(Tp.WonCasino.ToString());
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
            var result = 0;

            try
            {
                var time = _dateTimeHelper.GetTimeByMinuteRevert(iRangMins, timeZone, _timeFormat, DateTime.UtcNow);
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                foreach (var site in siteList)
                {
                    var _setting = _siteConfigDic[site.SiteId];

                    if (_setting == null) continue;

                    result = await _wagerWonCasinoBLL.GetWagerAsync(_setting, time.startUnix, time.endUnix);
                }

                if (result >= 0)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(3);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
         }
    }
}
