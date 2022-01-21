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
       
        public void InsertRequestLog(string tpName, string url, string input, string output)
        {
            LogContext.PushProperty("PrefixName", "Request");
            LogContext.PushProperty("Tp", tpName);
            var content = $"Url={url}, Input={input}, Output={output}";
            _logger.LogInformation(content);
        }

        public void InsertResponseLog(string tpName, string url, string input, string output, bool error = true)
        {
            LogContext.PushProperty("PrefixName", "Response");
            LogContext.PushProperty("Tp", tpName);
            var content = $"Url={url}, Input={input}, Output={output}";
            if (error)
            {
                _logger.LogError(content);
            }
            else
            {
                _logger.LogInformation(content);
            }
        }

        public void InsertHttpErrorLog(string tpName, dynamic statusCode, dynamic reasonPhrase)
        {
            LogContext.PushProperty("Tp", tpName);
            _logger.LogError($"StatusCode={statusCode}, ReasonPhrase={reasonPhrase}");
        }
    }
}
