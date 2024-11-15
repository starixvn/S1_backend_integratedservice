using System;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.PG;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.PG;

public class TicketPG : BackgroundService
{
    private Timer timer;
    private readonly ILogger<TicketPG> _logger;
    private readonly TicketPGBLL _ticketBLL;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly int _tpId = (int)Tp.PG;
    private readonly string _tpName = Tp.PG.ToString();
    private readonly int _tpType = (int)WagerTimerType.UnSettle;
    private readonly int _lockPeriod = -30; 

    public TicketPG(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<TicketPG>>();
        _ticketBLL = provider.GetRequiredService<TicketPGBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
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
            var ticketTime = _ticketBLL.GetTicketTime(execTime.StartTime, _lockPeriod, out bool exec);
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
            timer?.Change(dueTimeSpan, TimeSpan.Zero);
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

            var pendingList = _ticketBLL.GetPendingList(ticketTime);
            if (pendingList == null)
            {
                _logger.LogInformation($"Refund Worker Execute End {_tpId}, Count=0");
                return true;
            }

            // process
            foreach (var pending in pendingList)
            {
                var check = await _ticketBLL.CheckTicketStatus(pending, (int)gameType);
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
