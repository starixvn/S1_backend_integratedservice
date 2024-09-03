using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerN2Live : BackgroundService
    {
        private readonly ILogger<WagerN2Live> _logger;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerN2LiveBLL _wagerN2LiveBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
        private Timer timer;
        private readonly int intervalMins = 2;
        private readonly int iRangMins = 20;
        private readonly int tpId = (int)Tp.N2Live;
        private readonly string _tpName = Tp.N2Live.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int beyond = 6;
        private readonly int timeZone = 0;

        public WagerN2Live(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerN2Live>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerN2LiveBLL = provider.GetRequiredService<WagerN2LiveBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(Tp.N2Live.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                quick = 0;
                var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
                foreach (var siteData in siteList)
                {
                    if (siteData.TicketKey == 0)
                    {
                        _logger.LogError($"SiteId={siteData.SiteId}, no execute");
                        continue;
                    }

                    var setting = _siteConfigDic[siteData.SiteId];
                    var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, tpType, siteData.SiteId);
                    DateTime timerTime;
                    int last;
                    if (dataTpTimer == null)
                    {
                        last = iRangMins - 1;
                        timerTime = DateTime.Now.AddMinutes(-1 * last);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime;
                        last = iRangMins - 1;
                    }
                    if (timerTime < DateTime.Now)
                    {
                        var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);

                        var timeLimit = DateTime.UtcNow - DateTime.Parse(time.endTime);
                        if (timeLimit.TotalMinutes < 5)
                        {
                            // 日期值不能为未来日期 时间必须是当前时间的15分钟之前
                            time.endTime = DateTime.Parse(time.endTime).AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
                            time.endUnix -= 300;
                        }
                        var result = await _wagerN2LiveBLL.GetWagersAsync(tpId, setting["ApiUrl"], setting["VendorId"], setting["MerchantPassCode"], time.startTime, time.endTime);
                        if (result)
                        {
                            _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, siteData.SiteId);
                        }
                    }
                    var ts = DateTime.Now - timerTime;
                    var diffMin = (int)ts.TotalMinutes;
                    if (diffMin > beyond)
                    {
                        quick = 1;
                    }
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
                    var dueTimeSpan = TimeSpan.FromSeconds(45);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}