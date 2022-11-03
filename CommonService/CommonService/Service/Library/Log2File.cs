using Serilog.Context;

namespace PaymentService.Service.Library
{
    public class Log2File
    {
        private readonly ILogger<Log2File> _logger;
        public Log2File(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<Log2File>>();
        }
        public void Info(string infoContent, string PrefixName)
        {
            LogContext.PushProperty("PrefixName", PrefixName);
            _logger.LogInformation(infoContent);
        }
        public void Error(string errorContent, string PrefixName, Exception ex)
        {
            LogContext.PushProperty("PrefixName", PrefixName);
            _logger.LogError(ex, errorContent);
        }

    }
}
