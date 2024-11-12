using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TicketOne789 : BackgroundService
    {
        private Timer _timer35;
        private Timer _timer36;
        private Timer _timer37;
        private readonly ILogger<TicketOne789> _logger;
        private readonly TicketOne789BLL _ticketOne789BLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IHostEnvironment _env;
        private readonly int _tpId = (int)Tp.One789;
        private readonly string _tpName = Tp.One789.ToString();
        private readonly int _tpType = (int)WagerTimerType.UnSettle;
        private readonly int _lockPeriod = -30;// 近30分資料不處理

        public TicketOne789(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TicketOne789>>();
            _ticketOne789BLL = provider.GetRequiredService<TicketOne789BLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _env = provider.GetRequiredService<IWebHostEnvironment>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer35 = new Timer(DoRefund, GameType.One789_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            _timer36 = new Timer(DoRefund, GameType.One789_Lottery_789, TimeSpan.Zero, TimeSpan.Zero);
            _timer37 = new Timer(DoRefund, GameType.One789_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoRefund(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var task = true;
            var gameType = (GameType)state;

            if (!_env.IsProduction())
            {
                return;
            }

            try
            {
                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimerReferenceByGameType(_tpId, _tpType, (int)gameType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.Now.AddHours(-1) : dataTpTimer.StartTime,
                };

                // get time 
                TimeCast ticketTime = _ticketOne789BLL.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
                if (exec)
                {
                    var res = await ExecRefundAsync(gameType, ticketTime);
                    // update timer
                    if (res)
                    {
                        _thirdPartyBLL.SetThirdPartyTimerByGameType(_tpId, _tpType, ticketTime.endUnix, (int)gameType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (!task) ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(10);
                switch ((GameType)state)
                {
                    case GameType.One789_Lottery:
                        dueTimeSpan = TimeSpan.FromHours(2);
                        if (!task)
                        {
                            dueTimeSpan = TimeSpan.FromHours(1);
                        }
                        _timer35.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_Lottery_789:
                        _timer36.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_LiveCasino:
                        _timer37.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<bool> ExecRefundAsync(GameType gameType, TimeCast ticketTime)
        {
            var result = true;
            try
            {
                _logger.LogInformation($"Refund Worker Execute Start {_tpId}, {JsonConvert.SerializeObject(ticketTime)}");

                // get list
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                // check tp status and only get first site to run
                if (!siteList.Any(x => x.Status == 1))
                {
                    return true;
                }

                var pendingList = _ticketOne789BLL.GetPendingList((int)gameType, ticketTime);
                if (pendingList == null)
                {
                    _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count=0");
                    return true;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = await _ticketOne789BLL.CheckTicketStatus(pending, (int)gameType);
                    if (!check)
                    {
                        result = false;
                    }
                }
                _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count={pendingList.Count()}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Refund Worker Execute End {_tpId}");
                _logger.LogError(ex, ex.Message);
                result = false;
            }

            return result;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer35?.Change(Timeout.Infinite, 0);
            _timer36?.Change(Timeout.Infinite, 0);
            _timer37?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
