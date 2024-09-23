using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TicketSabaPlay : BackgroundService
    {
        private readonly ILogger<TicketSabaPlay> _logger;
        private readonly TicketSabaPlayBLL _ticketSabaPlayBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.SabaPlay;
        private readonly string _tpName = Tp.SabaPlay.ToString();
        private Timer timer;

        public TicketSabaPlay(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TicketSabaPlay>>();
            _ticketSabaPlayBLL = provider.GetRequiredService<TicketSabaPlayBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoRefund, GameType.SabaPlay_Poker, TimeSpan.Zero, TimeSpan.Zero);
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

                var pendingList = _ticketSabaPlayBLL.GetPendingList((int)gameType);
                if (pendingList == null)
                {
                    return false;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = await _ticketSabaPlayBLL.CheckTicketStatus(pending, (int)gameType);
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
