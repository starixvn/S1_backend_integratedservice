using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Library
{
    public class Log2File
    {
        private readonly string LogFolder = "";
        private readonly ILogger<Log2File> _logger;
        public Log2File(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<Log2File>>();
            var environment = provider.GetRequiredService<IHostEnvironment>();
            LogFolder = $@"{environment.ContentRootPath}\Logs";
        }
        public void Info(string InfoContent, string PrefixName)
        {
            var filePath = $@"{LogFolder}\{PrefixName}_Info_{DateTime.Now.ToString("yyyyMMdd")}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{InfoContent}");
            });
        }
        public void Error(string ErrorContent, string PrefixName, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, ErrorContent, PrefixName);
            }
            else
            {
                _logger.LogError(ErrorContent, PrefixName );
            }

            var filePath = $@"{LogFolder}\{PrefixName}_Err_{DateTime.Now.ToString("yyyyMMdd")}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{ErrorContent}");
            });
        }
    }
}
