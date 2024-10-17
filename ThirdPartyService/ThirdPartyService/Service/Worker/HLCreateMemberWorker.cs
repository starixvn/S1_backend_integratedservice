using System.Diagnostics;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdParty.HL;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.BLL;

namespace ThirdPartyService.Service.Worker
{
    public class HLCreateMemberWorker : BackgroundService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<HLCreateMemberWorker> _logger;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly Dictionary<int, HLConfig> _siteHLConfig;
        private readonly int _tpId = (int)TpEnum.HaoLong;
        private Timer _timer;
        private List<int> _failCustIdList = new();

        public HLCreateMemberWorker(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<HLCreateMemberWorker>>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<HLConfig>(TpEnum.HaoLong.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoHLCreateMemberWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoHLCreateMemberWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            bool isError = false;
            _failCustIdList = new List<int>();

            if (CheckHaoLongBusyTime())
            {
                _logger.LogInformation($"HLCreateMemberWorker - HaoLong Busy Time");
                return;
            }

            try
            {
                var sw = Stopwatch.StartNew();

                _logger.LogInformation($"HLCreateMemberWorker Start.");

                var tpCurrency = _tpCustInfoBll.GetTpCurrency(_tpId);

                foreach (var siteId in _siteHLConfig.Select(siteIdKey => siteIdKey.Key))
                {
                    foreach (var tpCurrencyId in tpCurrency)
                    {
                        ProcessCustomer(siteId, tpCurrencyId.CurrencyId);
                    }
                }

                if (_failCustIdList.Any())
                {
                    _logger.LogInformation($"HLCreateMemberWorker-Create Member Fail List={JsonConvert.SerializeObject(_failCustIdList)}. ");
                }

                _logger.LogInformation($"HLCreateMemberWorker End.\r\n" +
                                       $"Finish Time(m: s.ms): {Math.Round(sw.Elapsed.TotalMinutes)}:{Math.Round(sw.Elapsed.TotalMinutes)}.{Math.Round(sw.Elapsed.TotalMilliseconds)}.");
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, "HLCreateMemberWorker");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(3);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        #region Private

        private bool CheckHaoLongBusyTime()
        {
            var utcNow = DateTime.UtcNow;
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var timeInGmtPlus8 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus8);
            return timeInGmtPlus8.TimeOfDay >= new TimeSpan(14, 55, 0) && timeInGmtPlus8.TimeOfDay <= new TimeSpan(20, 5, 0);
        }

        private void ProcessCustomer(int siteId, int tpCurrencyId)
        {
            var custIdList = _tpCustInfoBll.GetCustCurrencySiteIdWithoutSabaId(_tpId, siteId, tpCurrencyId);
            foreach (var custId in custIdList)
            {
                HandleCustomerCreation(custId);
            }
        }

        private void HandleCustomerCreation(int custId)
        {
            var result = _thirdPartyBll.CreateMember(custId, _tpId, true);
            if (result.ErrorCode is not ErrorCode.Success)
            {
                _failCustIdList.Add(custId);
                _logger.LogInformation($"HLCreateMemberWorker-Create Member Fail. custId={custId}. " +
                                       $"result={JsonConvert.SerializeObject(result)}");
            }
            Thread.Sleep(5 * 1000); //休息5秒
        }

        #endregion Private
    }
}