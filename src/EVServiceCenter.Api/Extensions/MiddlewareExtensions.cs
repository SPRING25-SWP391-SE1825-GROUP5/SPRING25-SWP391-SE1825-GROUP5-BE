using EVServiceCenter.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace EVServiceCenter.Api.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationErrorMiddleware>();
        }
    }
}
