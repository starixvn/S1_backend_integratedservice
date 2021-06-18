using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class MGHeaderFilter: ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var req = context.HttpContext.Request;
            var requestId = req.Headers["X-MGP-REQ-ID"];
            var token = req.Headers["X-MGP-TOKEN"];
            var timestamp = req.Headers["X-MGP-REQUEST-TIME"].ToString();

            await next();
            
            var srcTimestamp = long.Parse(timestamp);
            var nowTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            context.HttpContext.Response.Headers.Add("X-MGP-REQ-ID",requestId);
            context.HttpContext.Response.Headers.Add("X-MGP-RESPONSE-TIME", (nowTimestamp - srcTimestamp).ToString());
        }
    }
}