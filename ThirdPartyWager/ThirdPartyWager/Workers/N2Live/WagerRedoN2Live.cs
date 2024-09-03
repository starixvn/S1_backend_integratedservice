using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoN2Live : BackgroundService
    {
        private readonly ILogger<WagerRedoN2Live> _logger;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerN2LiveBLL _wagerN2LiveBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
        private Timer timer;
        private readonly int intervalDays = 1;
        private readonly int iRangMins = 1440;
        private readonly int tpId = (int)Tp.N2Live;
        private readonly string _tpName = Tp.N2Live.ToString();
        private readonly int tpType = (int)WagerTimerType.RedoWager;
        private int quick = 0;
        private readonly int iDays = 2;
        private readonly int timeZone = 0;

        public WagerRedoN2Live(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerRedoN2Live>>();
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

                    if (dataTpTimer == null)
                    {
                        timerTime = DateTime.Now.AddDays(-7);
                    }
                    else
                    {
                        timerTime = dataTpTimer.StartTime.AddSeconds(1);
                    }

                    var time = _dateTimeHelper.GetTimeByDay(iRangMins, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    var cvTime = DateTimeOffset.Now.AddDays(iDays * -1).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();
                    if (time.endUnix < cvTime)
                    {
                        var result = await _wagerN2LiveBLL.GetWagersAsync(tpId, setting["ApiUrl"], setting["VendorId"], setting["MerchantPassCode"], time.startTime, time.endTime);
                        if (result)
                        {
                            _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, tpType, time.endUnix, siteData.SiteId);
                        }
                    }
                    var diffDay = (int)(DateTime.Now.Date - timerTime.Date).TotalDays;
                    if (diffDay >= (iDays + 1))
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
                    var dueTimeSpan = TimeSpan.FromSeconds(60);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromDays(intervalDays);
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