using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Workers
{
    public class WagerTFG : BackgroundService
    {
        private readonly ILogger<WagerTFG> _logger;
        private Timer timer;
        private readonly int tpId = (int)Tp.TFG;
        private readonly string _tpName = Tp.TFG.ToString();
        private readonly CommonBLL _commonBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int dataRangeMins = 15;
        private readonly int intervalMins = 1;
        private readonly int intervalSec = 5;
        private readonly int tpType = (int)WagerTimerType.Wager;
        private readonly DateTimeHelper _dateTimeHelper;
        private int quick = 0;
        private readonly int timeZone = 0;
        private readonly WagerTFGBLL _wagerTFGBLL;
        private readonly Dictionary<int, TFGConfig> _siteConfigDic;

        public WagerTFG(
            IServiceProvider provider
        )
        {
            _logger = provider.GetRequiredService<ILogger<WagerTFG>>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerTFGBLL = provider.GetRequiredService<WagerTFGBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<TFGConfig>(Tp.TFG.ToString());
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
                var TpCurrencyList = _commonBLL.GetTpCurrencyList((int)Tp.TFG);
                var config = _siteConfigDic[0];
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(tpId, tpType);
                var apiurl = config.ApiUrl;
                var SetTimer = true;

                DateTime timerTime;
                int last;
                if (dataTpTimer == null)
                {
                    last = dataRangeMins - 1;
                    timerTime = DateTime.Now.AddMinutes(-1 * last);
                }
                else
                {
                    timerTime = dataTpTimer.StartTime.AddSeconds(-1);
                    last = dataRangeMins - 1;
                }
                if (timerTime < DateTime.Now)
                {
                    var time = _dateTimeHelper.GetTimeByMinute(last, timeZone, "yyyy-MM-ddTHH:mm:ss.fffZ", timerTime);
                    foreach (var TpCurrency in TpCurrencyList)
                    {
                        if (config.CurrencyData.ContainsKey(TpCurrency.TpCurrencyId))
                        {
                            var result = await _wagerTFGBLL.GetWagersAsync(apiurl, config.CurrencyData[TpCurrency.TpCurrencyId].PrivateToken, time.startTime, time.endTime);
                            if (!result)
                            {
                                SetTimer = false;
                            }
                        }
                    }
                    if (SetTimer)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(tpId, tpType, time.endUnix);
                    }
                }
                var ts = DateTime.Now - timerTime;
                var diffMin = (int)ts.TotalMinutes;
                if (diffMin >= 10)
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
                    var dueTimeSpan = TimeSpan.FromSeconds(intervalSec);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
                else
                {
                    var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                    timer.Change(dueTimeSpan, TimeSpan.Zero);
                }
            }
        }
    }
}
