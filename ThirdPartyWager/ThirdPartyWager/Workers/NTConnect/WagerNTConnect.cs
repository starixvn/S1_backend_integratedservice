using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNTConnect : BackgroundService
    {
        private readonly ILogger<WagerNTConnect> _logger;
        private readonly WagerNTConnectBLL _wagerNTConnectBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _tpId = (int)Tp.NTConnect;
        private readonly WagerNTConnectService _wagerNTConnectService;
        private readonly WorkerType workerType = WorkerType.MainWager;
        private Timer timer;
        private readonly int intervalMins = 15;
        private readonly int iRangMins = 10;
        private readonly int tpId = (int)Tp.NTConnect;
        private readonly string _tpName = Tp.NTConnect.ToString();
        private readonly int tpType = (int)WagerTimerType.Wager;
        private int quick = 0;
        private readonly int timeZone = 8;

        public WagerNTConnect(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerNTConnect>>();
            _wagerNTConnectBLL = provider.GetRequiredService<WagerNTConnectBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _wagerNTConnectService = provider.GetRequiredService<WagerNTConnectService>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoHLReportTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoHLReportTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            
            try
            {
                var nowTime = DateTime.Now;
                _logger.LogInformation($"HaoLong Start DoHLReportTask Job Time : : +0 {nowTime:yyyy-MM-ddTHH:mm:ss}");

                var result = false;
                quick = 0;
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                
                var timerTime = dataTpTimer?.StartTime ?? nowTime.AddMinutes(-(iRangMins - 1));
                var last = (dataTpTimer == null) ? iRangMins - 1 : iRangMins;
                
                if (CheckHaoLongTime(1))
                {
                    // 14:55 ~ 22:30不拉單
                    _logger.LogInformation($"TP - HaoLong Busy Time");
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                    _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    return;
                }
                
                if (timerTime < nowTime)
                {
                     var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                     
                     //測試發現，下注時間跟開彩時間有差異，所以要抓7天的開彩日期，要不然會有下注單當天沒回來的情形發生。
                     var currentDate = DateTime.Parse(time.startTime);
                     var nextSevenDays = Enumerable.Range(0,8)
                         .Select(day => currentDate.AddDays(day - 1))
                         .ToList();
                     
                     //拿UnSettle資料
                     var ticketKeyHowLong = _thirdPartyBLL.GetAllSiteTicketKey(workerType, _tpId);
                     var unSettledWagerHLList = _wagerNTConnectService.GetHLBetUnSettledList(ticketKeyHowLong);
                     
                     result =  await _wagerNTConnectBLL.GetWagersAsync(unSettledWagerHLList,nextSevenDays);
                     
                     //有無單都要往前走 要不然會導致其餘排程問題出現
                     _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    
                     if(CheckHaoLongTime(2))
                     { 
                         //21:30 ~ 23:59開始結算
                       _logger.LogInformation($"HaoLong Start SetSettledWager Job Time : +0 = {DateTime.Now:yyyy-MM-ddTHH:mm:ss}");
                       _ = await _wagerNTConnectBLL.SetSettledWager();
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
                    var dueTimeSpan = TimeSpan.FromMinutes(1);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }

            }
        }

        private bool CheckHaoLongTime(int timeType)
        {
            //1:拉單
            //2:結算時間
            
            _logger.LogInformation($"HaoLong Start CheckHaoLongTime Job Time : Type [{timeType}] | +0 = {DateTime.Now:yyyy-MM-ddTHH:mm:ss}");
            var utcNow = DateTime.UtcNow;
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var timeInGmtPlus8 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus8);
            return timeType switch
            {
                1 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(14, 55, 0) &&
                     timeInGmtPlus8.TimeOfDay <= new TimeSpan(22, 30, 0),
                2 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(21, 30, 0) &&
                     timeInGmtPlus8.TimeOfDay <= new TimeSpan(23, 59, 0),
                _ => false
            };}
    }
}