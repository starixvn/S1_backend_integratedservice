using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            _logger.LogInformation(InfoContent, new string[] { PrefixName });
            var filePath = $@"{LogFolder}\{PrefixName}_Info_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{InfoContent}");
            });
        }
        public void Error(string ErrorContent, string PrefixName, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, ErrorContent, new string[] { PrefixName });
            }
            else
            {
                _logger.LogError(ErrorContent, new string[] { PrefixName });
            }
                            
                          
            var filePath = $@"{LogFolder}\{PrefixName}_Err_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{ErrorContent}");
            });
        }

    }
}
