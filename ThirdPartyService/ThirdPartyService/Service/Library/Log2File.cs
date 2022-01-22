using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ThirdPartyService.Service.Library
{
    public class Log2File
    {
        private readonly ILogger<Log2File> _logger;
        public Log2File(ILogger<Log2File> logger)
        {
            _logger = logger;
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
