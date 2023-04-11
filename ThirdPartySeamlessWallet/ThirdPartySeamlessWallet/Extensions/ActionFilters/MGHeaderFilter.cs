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
            var timestamp = req.Headers["X-MGP-REQUEST-TIMEMS"].ToString();
            _logger.LogInformation($"X-MGP-REQ-ID:{requestId}, X-MGP-TOKEN:{token}, X-MGP-REQUEST-TIMEMS:{timestamp}");
            
            await next();
            
            var srcTimestamp = (timestamp.Length == 10) ? long.Parse(timestamp) * 1000 : long.Parse(timestamp);
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var ts = new TimeSpan(nowTimestamp - srcTimestamp);
            var respTime = Math.Floor(ts.TotalMilliseconds).ToString();
            _logger.LogInformation($"X-MGP-REQ-ID:{requestId}, X-MGP-RESPONSE-TIME:{respTime}");
            context.HttpContext.Response.Headers.Add("X-MGP-REQ-ID",requestId);
            context.HttpContext.Response.Headers.Add("X-MGP-RESPONSE-TIME", respTime);
        }
    }
}