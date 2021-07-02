using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.Worker
{
    public class GetBalanceWorker : BackgroundService
    {
        #region Fields

        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GetBalanceWorker> _logger;
        private readonly Log2File _log2File;
        private readonly ServerInfoBLL _serverInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        private Timer _timer;
        private int execCount = 0;

        private DateTime? lastestProcessTime = null;
        private bool firstRun = false;

        #endregion

        #region Constructors

        public GetBalanceWorker(IHostEnvironment environment,
                                IConfiguration configuration,
                                ILogger<GetBalanceWorker> logger,
                                 Log2File log2File,
                                ServerInfoBLL serverInfoBLL,
                                ThirdPartyBLL thirdPartyBLL)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _log2File = log2File;

            _serverInfoBLL = serverInfoBLL;
            _thirdPartyBLL = thirdPartyBLL;
        }

        #endregion

        #region Action

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWorker, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoWorker(object state)
        {
            Interlocked.Increment(ref execCount);

            if (execCount == 1)
            {
                try
                {
                    DateTime currentTime = DateTime.UtcNow;

                    if (VerifyParameter(currentTime, lastestProcessTime))
                    {
                        _logger.LogInformation($"GetBalanceWorker-Worker Start At {DateTime.UtcNow.AddHours(8)}");

                        //_log2File.Info($"GetBalanceWorker-Worker Start At {DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

                        if (currentTime.Hour == 0)
                        {
                            CreateTable(currentTime);
                        }

                        var tpIdList = _configuration.GetSection("GetTpBalanceParam:TpId").Get<IEnumerable<int>>();
                        var siteList = _serverInfoBLL.GetServerInfoList();
                        var dataResult = new List<TpCustBalanceModel>();

                        foreach (var tpId in tpIdList)
                        {
                            foreach (var site in siteList)
                            {
                                _thirdPartyBLL.GetBalance(tpId, site.SiteId, out var data);
                                dataResult.AddRange(data);
                            }
                        }

                        _thirdPartyBLL.InsertBalance(currentTime.ToString("yyyyMMdd"),
                                                     currentTime.ToString("yyyy-MM-dd HH:00:00"),
                                                     dataResult);

                        lastestProcessTime = DateTime.UtcNow;
                        firstRun = false;

                        _logger.LogInformation($"GetBalanceWorker-Worker End At {DateTime.UtcNow.AddHours(8)}");
                        //_log2File.Info($"GetBalanceWorker-Worker End At {DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

                    }

                    Thread.Sleep(_configuration.GetValue<int>("GetTpBalanceParam:ColdDownTime") * 1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                }
                finally
                {
                    TimeSpan dueTimeSpan = TimeSpan.FromSeconds(_configuration.GetValue<int>("GetTpBalanceParam:CheckTime"));
                    _timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
            Interlocked.Decrement(ref execCount);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 驗證排程參數
        /// </summary>
        /// <returns>是否執行排程</returns>
        private bool VerifyParameter(DateTime currentTime, DateTime? lastestProcessTime)
        {
            if (CheckFirstRun(currentTime, lastestProcessTime, out DateTime lastestRunTime))
            {
                firstRun = true;
            }

            this.lastestProcessTime = this.lastestProcessTime == null ? lastestRunTime : this.lastestProcessTime;

            int period = _configuration.GetValue<int>("GetTpBalanceParam:Period");

            switch (period)
            {
                case 1://by hour 間隔幾小時執行排程(整點)
                    int intervalHour = _configuration.GetValue<int>("GetTpBalanceParam:IntervalHour");

                    //如果是第一次跑排程，要在整點跑(0~10分內跑)
                    //如果不是第一次跑排程，則判斷上次執行小時(yyyyMmdd HH:00:00)與現在時間是否間隔N小時
                    if ((firstRun && currentTime.Minute >= 0 && currentTime.Minute <= 10)
                       || (!firstRun && currentTime.Subtract(lastestRunTime).Hours >= intervalHour && (currentTime.Minute >= 0 && currentTime.Minute <= 20)))

                    {
                        return true;
                    }
                    break;

                case 2://by day 每天幾點執行排程(整點)
                    int specifiedHour = _configuration.GetValue<int>("GetTpBalanceParam:SpecifiedHour");
                    if (currentTime.Date != lastestRunTime.Date
                        && currentTime.AddHours(8).Hour == specifiedHour)
                    {
                        return true;
                    }
                    break;

                default:
                    _logger.LogWarning($"GetBalanceWorker-VerifyParameter-Warning_"
                                                   + " Parameter:Period is invalid.");
                    break;
            }


            return false;
        }

        /// <summary>
        /// 檢查是否為第一次執行排程
        /// </summary>
        /// <param name="currentTime">目前時間(UTC+0)</param>
        /// <param name="lastestProcessTime">server紀錄最近一次執行時間</param>
        /// <param name="lastestTime">實際上最後一次執行時間</param>
        /// <returns></returns>
        private bool CheckFirstRun(DateTime currentTime,
                                   DateTime? lastestProcessTime,
                                   out DateTime resultTime)
        {
            var baseTime = DateTime.Now;

            //如果Worker最近一次排程執行時間參數被reset,要先去找到最近一次排程執行時間
            if (lastestProcessTime == null)
            {
                var getResult = _thirdPartyBLL.GetLastestRunTime(currentTime.ToString("yyyyMMdd"));
                if (getResult != null)
                {
                    baseTime = Convert.ToDateTime(getResult);
                }
                else
                {
                    resultTime = baseTime;
                    return true; //can't find leastest run time.mean it first run.
                }
            }
            else
            {
                baseTime = Convert.ToDateTime(lastestProcessTime);
            }

            resultTime = new DateTime(baseTime.Year,
                                      baseTime.Month,
                                      baseTime.Day,
                                      baseTime.Hour,
                                      0, 0);

            return false; //already run.

        }

        /// <summary>
        /// 新增資料表
        /// </summary>
        /// <param name="currentTime">目前時間</param>
        private void CreateTable(DateTime currentTime)
        {
            if (!_thirdPartyBLL.CheckTpCustBalanceCourseTable(currentTime.ToString("yyyyMMdd")))
            {
                _thirdPartyBLL.CreateTpCustBalanceCourseTable(currentTime.ToString("yyyyMMdd"));

                _logger.LogInformation($"GetBalanceWorker-Create table "
                                      + $"TpCustBalanceCourse{currentTime.ToString("yyyyMMdd")} "
                                      + $"At {DateTime.UtcNow.AddHours(8)}");

                //_log2File.Info($"GetBalanceWorker-Create table "
                //                      + $"TpCustBalanceCourse{currentTime.ToString("yyyyMMdd")} "
                //                      + $"At {DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");
            }
        }

        #endregion
    }
}
