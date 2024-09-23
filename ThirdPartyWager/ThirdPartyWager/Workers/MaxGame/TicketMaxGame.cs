using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TicketMaxGame : BackgroundService
    {
        private readonly ILogger<TicketMaxGame> _logger;
        private readonly TicketMaxGameBLL _ticketMaxGameBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.MaxGame;
        private readonly string _tpName = Tp.MaxGame.ToString();
        private Timer timer;

        public TicketMaxGame(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TicketMaxGame>>();
            _ticketMaxGameBLL = provider.GetRequiredService<TicketMaxGameBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoRefund, GameType.MaxGame_Poker, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoRefund(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var task = true;
            var gameType = (GameType)state;

            try
            {
                var res = await ExecRefundAsync(gameType);
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

        private async Task<bool> ExecRefundAsync(GameType gameType)
        {
            var result = true;
            try
            {
                // get list
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);

                // check tp status and only get first site to run
                if (!siteList.Any(x => x.Status == 1))
                {
                    return false;
                }

                var pendingList = _ticketMaxGameBLL.GetPendingList((int)gameType);
                if (pendingList == null)
                {
                    return false;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = await _ticketMaxGameBLL.CheckTicketStatus(pending, (int)gameType);
                    if (!check)
                    {
                        result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                result = false;
            }

            _logger.LogInformation($"Refund Worker Execute End {_tpId}");
            return result;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
