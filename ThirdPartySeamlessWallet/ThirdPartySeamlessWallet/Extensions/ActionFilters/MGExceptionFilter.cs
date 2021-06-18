using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class MGExceptionFilter: IExceptionFilter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MGExceptionFilter> _logger;

        public MGExceptionFilter(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<MGExceptionFilter>>();
        }
        public void OnException(ExceptionContext context)
        {
            var exceptionType = context.Exception.GetType();
            context.ExceptionHandled = true;

            HttpStatusCode status;
            var exMessage = context.Exception.Message;
            if (exceptionType == typeof(MGException))
            {
                var ex = context.Exception as MGException;

                status = HttpStatusCode.BadRequest;
                _logger.LogInformation(exMessage);
            }
            else
            {
                status = HttpStatusCode.InternalServerError;
                _logger.LogError(context.Exception, $"Ex massage: {exMessage}, StackTrace: {context.Exception.StackTrace}");
            }

            var response = context.HttpContext.Response;
            response.StatusCode = (int)status;
        }
    }
}