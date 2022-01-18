using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class TpRefundWorker : BackgroundService
    {
        private readonly ILogger<TpRefundWorker> _logger;
        private readonly IHostEnvironment _environment;
        private readonly TpRefundBLL _tpRefundBLL;

        // timer{gameType} 
        private Timer timer8;
        private Timer timer35;
        private Timer timer36;
        private Timer timer37;

        public TpRefundWorker(
            IServiceProvider provider,
            IHostEnvironment environment
        )
        {
            _logger = provider.GetRequiredService<ILogger<TpRefundWorker>>();
            _tpRefundBLL = provider.GetRequiredService<TpRefundBLL>();
            _environment = environment;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer8 = new Timer(DoTask, GameType.AG_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            timer35 = new Timer(DoTask, GameType.One789_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timer36 = new Timer(DoTask, GameType.One789_Lottery_789, TimeSpan.Zero, TimeSpan.Zero);
            timer37 = new Timer(DoTask, GameType.One789_LiveCasino, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            bool task = true;
            var gametype = (GameType)state;
            try
            {
                task = await _tpRefundBLL.ExecRefundWorker((int)gametype);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(20);
                if (!task)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(5);
                }
                switch ((GameType)state)
                {
                    case GameType.AG_LiveCasino:
                        timer8.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_Lottery:
                        timer35.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_Lottery_789:
                        timer36.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.One789_LiveCasino:
                        timer37.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        break;
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer8?.Change(Timeout.Infinite, 0);
            timer35?.Change(Timeout.Infinite, 0);
            timer36?.Change(Timeout.Infinite, 0);
            timer37?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
