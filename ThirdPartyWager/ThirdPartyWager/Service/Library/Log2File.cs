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

        public void AddRequestLog(string tpName, string guid, string url, dynamic param, dynamic extra = null )
        {
            var logName = $"{tpName}_Request";
            var req = new
            {
                Guid = guid,
                Url = url,
                Param = param,
                Extra = extra,
            };

            Info(JsonConvert.SerializeObject(req), logName);
        }

        public void AddResponseLog(string tpName, string guid, string url, dynamic param, dynamic response, string logType = "Error", dynamic header = null, dynamic extra = null)
        {
            var logName = $"{tpName}_Response";
            var resp = new
            {
                Guid = guid,
                Url = url,
                Header = header,
                Param = param,
                Response = response,
                Extra = extra,
            };

            LogContext.PushProperty("Tp", tpName);
            if (string.Equals(logType, "Error", StringComparison.OrdinalIgnoreCase))
            {
                if (response.StatusCode != null)
                {
                    _logger.LogError($"StatusCode : {response.StatusCode}");
                }
                if (response.ReasonPhrase != null)
                {
                    _logger.LogError($"ReasonPhrase : {response.ReasonPhrase}");
                }
                Error(JsonConvert.SerializeObject(resp), logName);
            }
            else
            {
                Info(JsonConvert.SerializeObject(resp), logName);
            }
        }

        public void AddResultLog(string tpName, string guid, string message, string response, string logType = "Error", string exception = null)
        {
            var logName = $"{tpName}_Result";
            var res = new
            {
                Guid = guid,
                Message = message,
                Response = response,
                Exception = exception
            };

            LogContext.PushProperty("Tp", tpName);
            if (string.Equals(logType, "Error", StringComparison.OrdinalIgnoreCase))
            {
                Error(JsonConvert.SerializeObject(res), logName);
            }
            else
            {
                Info(JsonConvert.SerializeObject(res), logName);
            }
        }
    }
}
