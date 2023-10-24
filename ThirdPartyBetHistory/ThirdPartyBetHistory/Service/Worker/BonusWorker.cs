using ThirdPartyBetHistory.Service.BLL;

namespace ThirdPartyBetHistory.Service.Worker
{
    public class BonusWorker : BackgroundService
    {
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;
        private readonly ILogger<BonusWorker> logger;
        private Timer timer;
        public BonusWorker(IHostEnvironment environment, ILogger<BonusWorker> logger, SettledBLL settledBLL)
        {
            this.settledBLL = settledBLL;
            this.environment = environment;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoTask(object state)
        {
            try
            {
                // foreach (var bonus in settledBLL.GetBonusFulfill())
                // {
                //     settledBLL.BonusSettle(bonus);
                // }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error: Bonus Service");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
