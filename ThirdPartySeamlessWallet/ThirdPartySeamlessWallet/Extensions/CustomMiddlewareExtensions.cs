using Microsoft.AspNetCore.Builder;
using ThirdPartySeamlessWallet.Extensions.Middlewares;

namespace ThirdPartySeamlessWallet.Extensions
{
    public static class CustomMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<RequestLogMiddleware>();

        public static IApplicationBuilder UseResponseLogMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<ResponseLogMiddleware>();
    }
}