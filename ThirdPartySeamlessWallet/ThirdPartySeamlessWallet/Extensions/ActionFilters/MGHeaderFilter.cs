using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class MGHeaderFilter: ActionFilterAttribute
    {
        private readonly ILogger<MGHeaderFilter> _logger;
        public MGHeaderFilter(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<MGHeaderFilter>>();;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var req = context.HttpContext.Request;
            var requestId = req.Headers["X-MGP-REQ-ID"];
            var token = req.Headers["X-MGP-TOKEN"];
            var timestamp = req.Headers["X-MGP-REQUEST-TIME"].ToString();
            _logger.LogInformation($"X-MGP-REQ-ID:{requestId}, X-MGP-TOKEN:{token}, X-MGP-REQUEST-TIME:{timestamp}");
            
            await next();
            
            var srcTimestamp = long.Parse(timestamp);
            var nowTimestamp = DateTime.UtcNow.Ticks;
            var ts = new TimeSpan(nowTimestamp - srcTimestamp);
            var respTime = Math.Floor(ts.TotalMilliseconds).ToString();
            _logger.LogInformation($"X-MGP-REQ-ID:{requestId}, X-MGP-RESPONSE-TIME:{respTime}");
            context.HttpContext.Response.Headers.Add("X-MGP-REQ-ID",requestId);
            context.HttpContext.Response.Headers.Add("X-MGP-RESPONSE-TIME", respTime);
        }
    }
}