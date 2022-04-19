using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace ThirdPartySeamlessWallet.Extensions.Middlewares
{
    public class RequestLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
        }

        public async Task Invoke(HttpContext context)
        {
            var url = context.Request.GetDisplayUrl();
            if (context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
            }
            else
            {               
                var originalRequestBody = context.Request.Body;
                var content = string.Empty;
                if (context.Request.ContentType?.StartsWith("application/x-www-form-urlencoded") ?? false)
                {
                    var formData = await context.Request.ReadFormAsync();
                    content = JsonConvert.SerializeObject(formData);
                }
                var requestBodyStream = new MemoryStream();
                await context.Request.Body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);

                using (var requestBody = new StreamReader(requestBodyStream, Encoding.UTF8, true, 1024, true))
                {
                    var codePage = requestBody.CurrentEncoding.CodePage;
                
                    content = !context.Request.Method.Equals("GET",StringComparison.OrdinalIgnoreCase)
                              && string.IsNullOrEmpty(content)
                        ? await requestBody.ReadToEndAsync()
                        : content;
                    if (context.Request.RouteValues.TryGetValue("controller", out var controller))
                    {
                        LogContext.PushProperty("TP", controller.ToString());
                    }

                    if (context.Request.RouteValues.TryGetValue("action", out var action))
                    {
                        LogContext.PushProperty("Action", action.ToString());
                    }

                    var header = "";
                    if (context.Request.Headers != null)
                    {
                        header = JsonConvert.SerializeObject(context.Request.Headers);
                    }

                    _logger.LogInformation(
                        $"REQUEST METHOD: {context.Request.Method}, CodePage: {codePage}, REQUEST BODY: {content}, REQUEST Header: {header}, REQUEST URL: {url}");
                }

                requestBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;

                await _next(context);
                context.Request.Body = originalRequestBody;
            }
        }
    }
}