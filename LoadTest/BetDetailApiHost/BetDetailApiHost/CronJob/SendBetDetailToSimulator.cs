using BetDetailApiHost.ApiClient;
using BetDetailApiHost.Helper;

namespace BetDetailApiHost.CronJob
{
    public class SendBetDetailToSimulator(IConfiguration configuration, IMemoryDb memoryDb, ISabaSportApi sabaSportApi) : BackgroundService, IDisposable
    {
        private readonly int _intervalInSeconds = 30;

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
                var betDetails = await memoryDb.GetBetDetailsAsync();
                if (betDetails.Count > 0)
                {
                    Console.WriteLine($"Ticket Count: {betDetails.Count}");
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