using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Library
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
            var filePath = $@"{LogFolder}\{PrefixName}_Info_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{InfoContent}\r\n");
            });
        }
        public void Error(string ErrorContent, string PrefixName)
        {
            var filePath = $@"{LogFolder}\{PrefixName}_Err_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{ErrorContent}\r\n");
            });
        }

        public void AddRequestLog(string logName, string guid, string url, dynamic param, dynamic extra = null )
        {
            logName = $"{logName}_Request";
            var req = new
            {
                Guid = guid,
                Url = url,
                Param = param,
                Extra = extra,
            };

            Info(JsonConvert.SerializeObject(req), logName);
        }

        public void AddResponseLog(string logName, string guid, string url, dynamic param, dynamic response, string logType = "Error", dynamic header = null, dynamic extra = null)
        {
            var tp = logName;
            logName = $"{logName}_Response";
            var resp = new
            {
                Guid = guid,
                Url = url,
                Header = header,
                Param = param,
                Response = response,
                Extra = extra,
            };

            if (string.Equals(logType, "Error", StringComparison.OrdinalIgnoreCase))
            {
                LogContext.PushProperty("Tp", tp);
                _logger.LogError(JsonConvert.SerializeObject(resp));
                Error(JsonConvert.SerializeObject(resp), logName);
            }
            else
            {
                Info(JsonConvert.SerializeObject(resp), logName);
            }
        }

        public void AddResultLog(string logName, string guid, string message, string response, string logType = "Error", string exception = null)
        {
            var tp = logName;
            logName = $"{logName}_Result";
            var res = new
            {
                Guid = guid,
                Message = message,
                Response = response,
                Exception = exception
            };

            if (string.Equals(logType, "Error", StringComparison.OrdinalIgnoreCase))
            {
                LogContext.PushProperty("Tp", tp);
                _logger.LogError(JsonConvert.SerializeObject(res));
                Error(JsonConvert.SerializeObject(res), logName);
            }
            else
            {
                Info(JsonConvert.SerializeObject(res), logName);
            }
        }
    }
}
