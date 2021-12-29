using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;

namespace ThirdPartySeamlessWallet.Service.Library
{
    public class Log2File
    {
        private readonly ILogger<Log2File> _logger;
        public Log2File(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<Log2File>>();
        }

        public void Info(string InfoContent, string PrefixName)
        {
            LogContext.PushProperty("PrefixName", PrefixName);
            _logger.LogInformation(InfoContent);
        }

        public void Error(string ErrorContent, string PrefixName, Exception ex = null)
        {
            LogContext.PushProperty("PrefixName", PrefixName);
            if (ex != null)
            {
                _logger.LogError(ex, ErrorContent);
            }
            else
            {
                _logger.LogError(ErrorContent);
            }
        }
    }
}
