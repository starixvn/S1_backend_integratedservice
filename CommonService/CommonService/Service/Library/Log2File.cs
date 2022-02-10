using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;

namespace PaymentService.Service.Library
{
    public class Log2File
    {
        private readonly string LogFolder = "";
        private readonly ILogger<Log2File> _logger;
        public Log2File(IServiceProvider provider)
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            LogFolder = $@"{environment.ContentRootPath}\Logs";
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
