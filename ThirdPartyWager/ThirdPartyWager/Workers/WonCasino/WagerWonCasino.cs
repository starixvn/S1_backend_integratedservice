using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
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
        private readonly int timeZone = 8;
        private readonly int iRangMins = 5;
        private int quick = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int intervalMins = 5;
        
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
            var nowTime = DateTime.Now;

            try
            {
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                

                if (timerTime < nowTime)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, _timeFormat, timerTime);

                    var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                    foreach (var site in siteList)
                    {
                        if (site.TicketKey == 0)
                        {
                            _logger.LogError($"SiteId={site.SiteId}, no execute");
                            continue;
                        }

                        var _setting = _siteConfigDic[site.SiteId];

                        if (_setting == null) continue;

                        result = await _wagerWonCasinoBLL.GetWagerAsync(_setting, time.startUnix, time.endUnix);
                        if (result < 0)
                        {
                            //第三方因素或Insert失敗回傳為-1，直接進break不再更新時間。
                            break;
                        }
                    }

                    if (result > 0)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }
                
                if ((int)DateTime.Now.Subtract(timerTime).TotalMinutes > 3)
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
                    var dueTimeSpan = TimeSpan.FromSeconds(10);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
         }
    }
}
