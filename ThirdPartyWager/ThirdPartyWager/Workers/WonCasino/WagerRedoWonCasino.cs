using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.TpSiteConfig;
using Serilog.Context;
using System.Collections.Generic;
using System;

namespace ThirdPartyWager.Workers
{
    public class WagerRedoWonCasino : BackgroundService
    {
        private readonly int _tpId = (int)Tp.WonCasino;
        private readonly int _tpType = (int)WagerTimerType.RedoWager;
        private readonly string _tpName = Tp.WonCasino.ToString();
        private Timer _timer;
        private readonly Dictionary<int, WonCasinoConfig> _siteConfigDic;
        private readonly WagerWonCasinoBLL _wagerWonCasinoBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerWonCasino> _logger;
        private readonly int timeZone = 0;
        private readonly int iRangMins = 30;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int intervalMins = 5;
        public WagerRedoWonCasino(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerWonCasino>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _wagerWonCasinoBLL = provider.GetRequiredService<WagerWonCasinoBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WonCasinoConfig>(Tp.WonCasino.ToString());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoUnsettleCheck, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoUnsettleCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                var nowDateTime = DateTime.Now;
                var result = 0;

                if (dataTpTimer == null)
                {
                    dataTpTimer = new ThirdPartyTimerModel()
                    {
                        TpId = _tpId,
                        Type = _tpType,
                        StartTime = nowDateTime.AddHours(-2),
                    };
                }
                else
                {
                    dataTpTimer.StartTime = dataTpTimer.StartTime.AddSeconds(1);
                }

                if (dataTpTimer.StartTime <= DateTime.Now.AddMinutes(-30))
                {
                    var time = _dateTimeHelper.GetTimeByMinute(iRangMins - 1, timeZone, _timeFormat, dataTpTimer.StartTime);
                    var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                    foreach (var site in siteList)
                    {
                        var _setting = _siteConfigDic[site.SiteId];

                        if (_setting == null) continue;

                        result = await _wagerWonCasinoBLL.GetWagerAsync(_setting, time.startUnix, time.endUnix);
                    }
                    
                    if (result > 0)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, time.endUnix);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
