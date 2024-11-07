using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerHL : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<WagerHL> _logger;
        private readonly WagerHLBLL _wagerHLBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly WagerHLService _wagerHLService;
        private readonly int _tpId = (int)Tp.HaoLong;
        private readonly string _tpName = Tp.HaoLong.ToString();
        private readonly WorkerType _workerType = WorkerType.MainWager;
        private readonly int _intervalMins = 60;
        private readonly int _iRangMins = 60;
        private readonly int _tpType = (int)WagerTimerType.Wager;
        private readonly int _timeZone = 8;

        public WagerHL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerHL>>();
            _wagerHLBLL = provider.GetRequiredService<WagerHLBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerHLService = provider.GetRequiredService<WagerHLService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoHLReportTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoHLReportTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var quick = 0;

            try
            {
                var nowTime = DateTime.Now;
                _logger.LogInformation($"HaoLong Start DoHLReportTask Job Time : : +0 {nowTime:yyyy-MM-ddTHH:mm:ss}");

                var result = false;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(_iRangMins - 1));
                var last = (dataTpTimer == null) ? _iRangMins - 1 : _iRangMins;

                if (timerTime < nowTime)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, _timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    //依照HaoLong三天兩頭都在維護 直接讓時間往前走 不然會導致其餘排程問題出現
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);

                    //測試發現，下注時間跟開彩時間有差異，抓3天的開彩日期即可，太多反而不好搞。
                    var currentDate = DateTime.Parse(time.startTime);
                    var nextSevenDays = Enumerable.Range(0, 4)
                        .Select(day => currentDate.AddDays(day - 1))
                        .ToList();

                    //拿UnSettle資料
                    var ticketKeyHowLong = _thirdPartyBLL.GetAllSiteTicketKey(_workerType, _tpId);
                    var unSettledWagerHLList = _wagerHLService.GetHLBetUnSettledList(ticketKeyHowLong, 10000);

                    result = await _wagerHLBLL.GetWagersAsync(unSettledWagerHLList, nextSevenDays);

                    if (result)
                    {
                        var tryResult = await _wagerHLBLL.TryGetSettledResult();
                        if (tryResult)
                        {
                            _ = await _wagerHLBLL.SetSettledWager(unSettledWagerHLList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(_intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}