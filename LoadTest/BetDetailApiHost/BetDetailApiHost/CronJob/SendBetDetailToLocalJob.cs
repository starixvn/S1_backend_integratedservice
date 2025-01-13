using BetDetailApiHost.ApiClient;
using BetDetailApiHost.Repository;

namespace BetDetailApiHost.CronJob
{
    public class SendBetDetailToLocalJob(IConfiguration configuration, IBetDetailStorage betDetailStorage, ISabaSportApi sabaSportApi) : BackgroundService, IDisposable
    {
        private readonly int _intervalInSeconds = 25;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("SendBetDetailToLocalJob Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await SendBetDetailAsync();
                await Task.Delay(TimeSpan.FromSeconds(_intervalInSeconds), stoppingToken);
            }
        }

        private async Task SendBetDetailAsync()
        {
            Console.WriteLine($"SendBetDetailToLocalJob running at: {DateTime.Now}, Endpoint: {configuration.GetConnectionString("Simulator")}");

            try
            {
                var betDetails = await betDetailStorage.GetBetDetailsAsync();
                Console.WriteLine($"Ticket Count: {betDetails.Count}");
                if (betDetails.Count > 0)
                {
                    var result = await sabaSportApi.BatchBetDetails(betDetails);
                }

                Console.WriteLine($"SendBetDetailToLocalJob done at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendBetDetailAsync: {ex.Message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SendBetDetailToLocalJob Service stopping...");
            return base.StopAsync(cancellationToken);
        }
    }
}