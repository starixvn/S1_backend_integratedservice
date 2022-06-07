using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerReSettlePT : BackgroundService
    {
        private readonly ILogger<WagerReSettlePT> _logger;
        private readonly WagerPTBLL _wagerPtBll;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly IHostEnvironment _environment;
        private Timer _timer;
        private readonly int _tpId = (int)Tp.PT;
        private readonly string _tpName = Tp.PT.ToString();

        public WagerReSettlePT(ILogger<WagerReSettlePT> logger, WagerPTBLL wagerPTBLL, DateTimeHelper dateTimeHelper, IHostEnvironment environment)
        {
            _logger = logger;
            _wagerPtBll = wagerPTBLL;
            _dateTimeHelper = dateTimeHelper;
            _environment = environment;
        }
        
        private IEnumerable<PTRequestModel> InitWagerList()
        {
            var list = new List<PTRequestModel>
            {
                new PTRequestModel {SiteId = (int)Site.Nova88, Currency = "MYR", TimeZone = 8},
                new PTRequestModel {SiteId = (int)Site.Nova88, Currency = "THB", TimeZone = 7},
                new PTRequestModel {SiteId = (int)Site.Nova88 , Currency = "IDR" , TimeZone = 8},
                new PTRequestModel {SiteId = (int)Site.Nova88 , Currency = "USD" , TimeZone = 0},
                new PTRequestModel {SiteId = (int)Site.Nova88 , Currency = "CNY" , TimeZone = 8},
                new PTRequestModel {SiteId = (int)Site.Nova88 , Currency = "INR" , TimeZone = 5},
                new PTRequestModel {SiteId = (int)Site.Goal123 , Currency = "VND" , TimeZone = 7},
                new PTRequestModel {SiteId = (int)Site.Bwing , Currency = "VND" , TimeZone = 7}
            };
            return list;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, InitWagerList(), TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private async void DoTask(object state)
        {
            LogContext.PushProperty("Tp", _tpName);
            
            if (!_environment.IsProduction())
            {
                return;
            }
            
            try
            {
                var list = (IEnumerable<PTRequestModel>)state;
                foreach (var task in list)
                {
                    LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
                    var timerTime = new DateTime(2022,04,01);
                    for (var day = 0; day < 31; day++)
                    {
                        for (var hour = 0; hour < 24; hour++)
                        {
                            for (var min = 0; min < 2; min++)
                            {
                                var time = _dateTimeHelper.GetTimeByMinute(29, task.TimeZone, "yyyy-MM-dd HH:mm:ss", timerTime);
                                try
                                {
                                    await _wagerPtBll.GetWagersAsync(_tpId, time.startTime, time.endTime, task.Currency, task.SiteId, task.TimeZone ,0,WagerType.WagerReSettle);
                                    timerTime = timerTime.AddMinutes(30);
                                }
                                catch (Exception e)
                                {
                                    var msg = $"Input Task: {JsonConvert.SerializeObject(task)} , Input Time:{JsonConvert.SerializeObject(time)} )";
                                    _logger.LogError(e, $"WagerReSettlePT {msg}\r\n{e.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WagerReSettlePT");
            }
            finally
            {
                _logger.LogInformation($"WagerReSettlePT FINISH");
            }
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}