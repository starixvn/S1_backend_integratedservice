using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.Nextspin;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Workers.Nextspin
{
    public class TicketNextspin : BackgroundService
    {
        private Timer timer;
        private readonly ILogger<TicketNextspin> _logger;
        private readonly TicketNextspinBll _ticketNextspinBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.Nextspin;
        private readonly string _tpName = Tp.Nextspin.ToString();
        private readonly int _tpType = (int)WagerTimerType.UnSettle;
        private readonly int _lockPeriod = -30;// 近30分資料不處理

        public TicketNextspin(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TicketNextspin>>();
            _ticketNextspinBll = provider.GetRequiredService<TicketNextspinBll>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(BetResultCheck, GameType.Nextspin_RNG, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void BetResultCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var gameType = (GameType)state;

            try
            {
                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = dataTpTimer?.StartTime ?? DateTime.Now.AddHours(-1),
                };

                // get time 
                var ticketTime = _ticketNextspinBll.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
                if (exec)
                {
                    var res = await ExecBetResultCheckAsync(gameType, ticketTime);
                    // update timer
                    if (res)
                    {
                        _thirdPartyBLL.SetThirdPartyTimer(_tpId, _tpType, ticketTime.endUnix);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<bool> ExecBetResultCheckAsync(GameType gameType, TimeCast ticketTime)
        {
            var result = true;
            try
            {
                _logger.LogInformation("Bet Result Check Worker Execute Start {TpId}, {TicketTime}", _tpId, JsonConvert.SerializeObject(ticketTime));

                // get list
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                // check tp status and only get first site to run
                if (siteList.All(x => x.Status != 1))
                {
                    return true;
                }

                var pendingList = _ticketNextspinBll.GetPendingList((int)gameType, ticketTime);
                if (pendingList == null)
                {
                    _logger.LogInformation("Bet Result Check Worker Execute End {TpId}, Count=0", _tpId);
                    return true;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = await _ticketNextspinBll.CheckTicketStatus(pending, (int)gameType);
                    if (!check)
                    {
                        result = false;
                    }
                }
                _logger.LogInformation($"Bet Result Check Worker Execute End {_tpId}, Count= {pendingList.Count()}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Bet Result Check Worker Execute End {_tpId}");
                _logger.LogError(ex, ex.Message);
                result = false;
            }

            return result;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
