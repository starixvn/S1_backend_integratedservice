using Serilog.Context;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class PGResettleWorker : BackgroundService
    {
        private readonly ILogger<PGResettleWorker> _logger;
        private readonly MySqlClient _mainDbClient;

        private Timer timer;
        private readonly int tpType = (int)WagerTimerType.UnSettle;
        private readonly int runTime = 1; // in minute
        private readonly string _tpName = Tp.PG.ToString();


        public PGResettleWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<PGResettleWorker>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                //var resettle = _mainDbClient.Query<WagerWMTotal>("Service_BettransRng_PG_Resettle_Fix_Get", new { BeginTime = beginTime, EndTime = endTime, Currency = currency });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                timer.Change(TimeSpan.FromMinutes(runTime), TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}