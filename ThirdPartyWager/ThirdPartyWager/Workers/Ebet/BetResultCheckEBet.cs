using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class BetResultCheckEBet : BackgroundService
    {
        private Timer timer;
        private readonly ILogger<BetResultCheckEBet> _logger;
        private readonly TicketEBetBLL _ticketEBetBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.EBet;
        private readonly string _tpName = Tp.EBet.ToString();
        private readonly int _tpType = (int)WagerTimerType.UnSettle;
        private readonly int _lockPeriod = -30;
        private readonly int _deductTime = -1;

        public BetResultCheckEBet(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<BetResultCheckEBet>>();
            _ticketEBetBLL = provider.GetRequiredService<TicketEBetBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(BetResultCheck, GameType.EBet_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void BetResultCheck(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var task = true;
            var gameType = (GameType)state;

            try
            {
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.Now.AddHours(-1) : dataTpTimer.StartTime,
                };

                TimeCast ticketTime = _ticketEBetBLL.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
                if (exec)
                {
                    var res = await ExecBetResultCheckAsync(gameType, ticketTime);
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
                var dueTimeSpan = (!task) ? TimeSpan.FromMinutes(3) : TimeSpan.FromMinutes(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<bool> ExecBetResultCheckAsync(GameType gameType, TimeCast ticketTime)
        {
            var result = true;
            try
            {
                _logger.LogInformation($"Bet Result Check Worker Execute Start {_tpId}, {JsonConvert.SerializeObject(ticketTime)}");
                
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                if (!siteList.Any(x => x.Status == 1))
                {
                    return true;
                }

                var pendingList = _ticketEBetBLL.GetPendingList((int)gameType, ticketTime);
                if (pendingList == null)
                {
                    _logger.LogInformation($"Bet Result Check Worker Execute End {_tpId}, Count=0");
                    return true;
                }

                foreach (var pending in pendingList)
                {
                    var check = await _ticketEBetBLL.CheckTicketStatus(pending, (int)gameType);
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
