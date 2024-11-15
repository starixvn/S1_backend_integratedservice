using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TicketRNGKeno : BackgroundService
    {
        private Timer _timer;
        private readonly ILogger<TicketRNGKeno> _logger;
        private readonly TicketRNGKenoBLL _ticketKenoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly int _tpId = (int)Tp.RNGKeno;
        private readonly string _tpName = Tp.RNGKeno.ToString();
        private readonly int _tpType = (int)WagerTimerType.UnSettle;
        private readonly int _lockPeriod = -30;// 近30分資料不處理

        public TicketRNGKeno(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TicketRNGKeno>>();
            _ticketKenoBLL = provider.GetRequiredService<TicketRNGKenoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoRefund, GameType.RNG_Keno, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoRefund(object state)
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
                var ticketTime = _ticketKenoBLL.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
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
                var dueTimeSpan = TimeSpan.FromMinutes(5);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
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
                if (siteList.All(x => x.Status != 1))
                {
                    return true;
                }

                var pendingList = _ticketKenoBLL.GetPendingList((int)gameType, ticketTime);
                if (pendingList == null)
                {
                    _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count=0");
                    return true;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = await _ticketKenoBLL.CheckTicketStatus(pending, (int)gameType);
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
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}