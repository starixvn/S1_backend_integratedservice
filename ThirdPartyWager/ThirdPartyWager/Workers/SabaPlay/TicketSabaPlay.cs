using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TicketSabaPlay : BackgroundService
    {
        private Timer timer;
        private readonly ILogger<TicketSabaPlay> _logger;
        private readonly TicketSabaPlayBLL _ticketSabaPlayBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.SabaPlay;
        private readonly string _tpName = Tp.SabaPlay.ToString();
        private readonly int _tpType = (int)WagerTimerType.UnSettle;
        private readonly int _lockPeriod = -30;// 近30分資料不處理
        private readonly int _deductTime = -1;

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
                // get timer
                var dataTpTimer = _thirdPartyBLL.GetThirdPartyTimer(_tpId, _tpType);

                // default time
                var execTime = new ThirdPartyTimerModel()
                {
                    TpId = _tpId,
                    Type = _tpType,
                    StartTime = (dataTpTimer == null) ? DateTime.Now.AddHours(-1) : dataTpTimer.StartTime,
                };

                // get time 
                TimeCast ticketTime = _ticketSabaPlayBLL.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
                if (exec)
                {
                    var res = await ExecRefundAsync(gameType, ticketTime);
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
                var dueTimeSpan = (!task) ? TimeSpan.FromMinutes(3) : TimeSpan.FromMinutes(5);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
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

                var pendingList = _ticketSabaPlayBLL.GetPendingList((int)gameType, ticketTime);
                if (pendingList == null)
                {
                    _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count=0");
                    return true;
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
                _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count= {pendingList.Count()}");
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
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
