using Serilog.Context;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerNineW : BackgroundService
    {
        private readonly ILogger<WagerNineW> _logger;
        private readonly NineWBLL _nineWService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private Timer _getDomainTimer;
        private Timer _settleTimer;
        private Timer _settleTimerByDate;
        private Timer _reSttleTimer;
        private Timer _reportTimer;
        private Timer _financeTimer;

        private readonly int tpId = (int)Tp.NineW;
        private readonly string _tpName = Tp.NineW.ToString();
        private readonly int tpType = 51;
        private readonly int TimeZone = 8;

        public WagerNineW(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerNineW>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _nineWService = provider.GetRequiredService<NineWBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            DoGetDomain();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _getDomainTimer = new Timer(DoGetDomain, null, TimeSpan.FromHours(1), TimeSpan.Zero);
            _settleTimer = new Timer(DoSettle, null, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            _settleTimerByDate = new Timer(DoSettleByDate, null, TimeSpan.FromSeconds(3), TimeSpan.Zero);
            _reSttleTimer = new Timer(DoReSettle, null, TimeSpan.FromMinutes(2), TimeSpan.Zero);
            _reportTimer = new Timer(GetReportByDate, null, TimeSpan.FromSeconds(7), TimeSpan.Zero);
            _financeTimer = new Timer(DoTpDailySummary, null, TimeSpan.FromSeconds(7), TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private async void DoGetDomain()
        {
            await _nineWService.QueryDomain();
        }
        
        private async void DoSettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.Wager;
            var dataRangeMins = 20;
            var nextTimeSpan = TimeSpan.FromMinutes(1);
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime.AddMinutes(-10) ?? DateTime.Now.AddDays(-1);
                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByMinute(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                    var isSuccess = await _nineWService.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    isSuccess = await _nineWService.GetUnsettleWagersAsync(timeCast.startUnix * 1000);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"NineW Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    nextTimeSpan = TimeSpan.FromSeconds(5);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettle.Exception");
                TimeSpan.FromMinutes(1);
            }
            finally
            {
                _settleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoSettleByDate(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.RedoWager;
            var dataRangeMins = 1441;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromDays(intervalDays);

            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.Date.AddDays(-3);

                if (thisTime < DateTime.Now)
                {
                    var timeCast = _dateTimeHelper.GetTimeByDay(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                    var isSuccess = await _nineWService.GetWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"NineW Get Wagers byDate: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoSettleByDate.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {
                _settleTimerByDate.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoReSettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.ReSettle;
            var dataRangeMins = 180;
            var intervalHours = 1;
            var nextTimeSpan = TimeSpan.FromDays(intervalHours);
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddDays(-3);

                var timeCast = _dateTimeHelper.GetTimeByHour(dataRangeMins, TimeZone, "yyyy-MM-dd HH:mm", thisTime);
                var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(TimeZone, 0, 0)).ToUnixTimeSeconds();
                if (timeCast.endUnix < cvTime)
                {
                    var isSuccess = await _nineWService.GetReSettleWagersAsync(timeCast.startTime, timeCast.endTime);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"NineW Get ReSettle Wagers : {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, timeCast.endUnix, tpType);
                    }
                }

                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
                {
                    nextTimeSpan = TimeSpan.FromMinutes(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoReSettle.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(5);
            }
            finally
            {
                _reSttleTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void GetReportByDate(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.UserReport;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromHours(4);
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var date = lastTime?.StartTime ?? DateTime.UtcNow.BeginningOfTheDay();
                var endTime = date.AddDays(1).AddMilliseconds(-1);
                if (endTime < DateTime.UtcNow)
                {
                    var summaryTransaction = _nineWService.GetSummaryTransactionReportAsync(date);//querySummaryTransaction
                    if (await summaryTransaction)
                    {
                        _logger.LogInformation($"NineW Get {date:yyyy-MM-dd} Report");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, new DateTimeOffset(date.AddHours(4)).ToUnixTimeSeconds(), tpType);
                    }
                }

                if ((int)DateTime.UtcNow.AddDays(-1).Subtract(date).TotalDays > intervalDays)
                {
                    nextTimeSpan = TimeSpan.FromSeconds(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.GetReportByDate.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {
                _reportTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        private async void DoTpDailySummary(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var timerType = (int)WagerTimerType.Finance;
            var intervalDays = 1;
            var nextTimeSpan = TimeSpan.FromDays(1);
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(tpId, timerType, tpType);
                var date = lastTime?.StartTime ?? DateTime.UtcNow.BeginningOfTheDay();
                var endTime = date.AddDays(1).AddMilliseconds(-1);
                if (endTime < DateTime.UtcNow)
                {
                    var countSummary = _nineWService.GetCountSummaryTransactionReportAsync(date);//countSummaryTransaction                   
                    if (await countSummary)
                    {
                        _logger.LogInformation($"NineW Get {date:yyyy-MM-dd} TpDailySummary");
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(tpId, timerType, new DateTimeOffset(date.AddDays(1)).ToUnixTimeSeconds(), tpType);
                    }
                }

                if ((int)DateTime.UtcNow.AddDays(-1).Subtract(date).TotalDays > intervalDays)
                {
                    nextTimeSpan = TimeSpan.FromSeconds(3);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoTpDailySummary.Exception");
                nextTimeSpan = TimeSpan.FromHours(1);
            }
            finally
            {
                _financeTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _settleTimer?.Change(Timeout.Infinite, 0);
            _settleTimerByDate?.Change(Timeout.Infinite, 0);
            _reSttleTimer?.Change(Timeout.Infinite, 0);
            _reportTimer?.Change(Timeout.Infinite, 0);
            _financeTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
        
        private async void DoGetDomain(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var nextTimeSpan = TimeSpan.FromHours(1);
            
            try
            {
                await _nineWService.QueryDomain();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerNineW.DoTpDailySummary.Exception");
                nextTimeSpan = TimeSpan.FromMinutes(10);
            }
            finally
            {
                _getDomainTimer.Change(nextTimeSpan, TimeSpan.Zero);
            }
        }
    }
}