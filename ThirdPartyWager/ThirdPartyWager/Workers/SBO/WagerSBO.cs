using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerSBO : BackgroundService
    {
        private readonly ILogger<WagerSBO> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ISBOBLL _sboBll;

        private const int TpId = (int)Tp.SBO;
        private const int TimeZone = -4;
        private Timer _sportsBookWagerTimer;
        private Timer _virtualSportsWagerTimer;

        private readonly Dictionary<GameType, SBOPortfolio> _gameTypeMap = new()
        {
            { GameType.SBO_Sports, SBOPortfolio.SportsBook },
            { GameType.SBO_VirtualSports, SBOPortfolio.VirtualSports }
        };

        public WagerSBO(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSBO>>();
            _sboBll = provider.GetRequiredService<ISBOBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _sportsBookWagerTimer = new Timer(QueryWager, GameType.SBO_Sports, TimeSpan.FromSeconds(1), TimeSpan.Zero);
            _virtualSportsWagerTimer = new Timer(QueryWager, GameType.SBO_VirtualSports, TimeSpan.FromMinutes(1), TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void QueryWager(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", Tp.SBO.ToString());
            var doQuickly = false;
            var gameType = (GameType)state;
            var portfolio = _gameTypeMap[gameType];
            const int dataRangeMins = 15;
            const int intervalMins = 1;
            const int intervalSec = 5;
            try
            {
                var lastTime = _thirdPartyBLL.GetThirdPartyTimer(TpId, (int)gameType);
                var thisTime = lastTime?.StartTime ?? DateTime.Now.AddMinutes(-1 * dataRangeMins);

                if (thisTime < DateTime.Now)
                {
                    thisTime = thisTime.AddMinutes(1);
                    var timeCast = _dateTimeHelper.GetTimeByMinuteRevert(dataRangeMins, TimeZone, "s", thisTime);
                    var isSuccess = await _sboBll.GetWagersAsync(timeCast.startTime, timeCast.endTime, portfolio);
                    if (isSuccess)
                    {
                        _logger.LogInformation($"{gameType} Get Wagers: {timeCast.startTime} ~ {timeCast.endTime}");
                        _thirdPartyBLL.SetThirdPartyTimer(TpId, (int)gameType, timeCast.endUnix);
                    }
                }
                if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > 5)
                {
                    doQuickly = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{gameType} WagerSBO.QueryWager.Exception");
            }
            finally
            {
                var nextTimeSpan = doQuickly ? TimeSpan.FromSeconds(intervalSec) : TimeSpan.FromMinutes(intervalMins);
                switch (portfolio)
                {
                    case SBOPortfolio.SportsBook:
                        _sportsBookWagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                    case SBOPortfolio.VirtualSports:
                        _virtualSportsWagerTimer.Change(nextTimeSpan, TimeSpan.Zero);
                        break;
                }
            }
        }
    }
}