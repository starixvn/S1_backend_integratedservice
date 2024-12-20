using System.Diagnostics;

using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.BLL;

namespace ThirdPartyService.Service.Worker
{
    public class SBSWCreateMemberWorker : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SBSWCreateMemberWorker> _logger;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly int _tpId = (int)TpEnum.SBSW;
        private Timer _timer;
        private List<int> _failCustIdList = new();

        public SBSWCreateMemberWorker(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<SBSWCreateMemberWorker>>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            bool isError = false;
            _failCustIdList = new List<int>();

            try
            {
                var sw = Stopwatch.StartNew();

                _logger.LogInformation($"SBSWCreateMemberWorker Start.");

                var custIdList = _tpCustInfoBll.GetCustIdWithoutSabaId(_tpId);

                foreach (var custId in custIdList)
                {
                    var result = _thirdPartyBll.CreateMember(custId, _tpId, true, false);
                    if (result.ErrorCode is not ErrorCode.Success)
                    {
                        if (result.RawErrorCode is "6") //Duplicate Vendor_Member_ID，補TpCustInfo
                        {
                            var duplicateResult = _thirdPartyBll.CreateMember(custId, _tpId, false, false);
                            if (result.ErrorCode is not ErrorCode.Success)
                            {
                                _failCustIdList.Add(custId);
                                _logger.LogInformation($"SBSWCreateMemberWorker-Create Member [Duplicate] Fail. custId={custId}. " +
                                                   $"result={JsonConvert.SerializeObject(result)}");
                                continue;
                            }
                        }
                        else if (result.RawErrorCode is "903") //System Is Under Maintenance 
                        {
                            _logger.LogInformation($"SBSWCreateMemberWorker-SBSW System Is Under Maintenance. ");
                        }
                        else
                        {
                            _failCustIdList.Add(custId);
                            _logger.LogInformation($"SBSWCreateMemberWorker-Create Member Fail. custId={custId}. " +
                                                   $"result={JsonConvert.SerializeObject(result)}");
                        }
                    }
                    Thread.Sleep(2 * 1000); //休息5秒
                }

                if (_failCustIdList.Any())
                {
                    _logger.LogInformation($"SBSWCreateMemberWorker-Create Member Fail List={JsonConvert.SerializeObject(_failCustIdList)}. ");
                }

                _logger.LogInformation($"SBSWCreateMemberWorker End.\r\n" +
                    $"Finish Time(m: s.ms): {Math.Round(sw.Elapsed.TotalMinutes)}:{Math.Round(sw.Elapsed.TotalMinutes)}.{Math.Round(sw.Elapsed.TotalMilliseconds)}.");

            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, "SBSWCreateMemberWorker");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(5);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromMinutes(3);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

    }
}
