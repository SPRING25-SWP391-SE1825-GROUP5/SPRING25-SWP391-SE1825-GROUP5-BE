using EVServiceCenter.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace EVServiceCenter.Api.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }

        public static IApplicationBuilder UseJsonErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JsonErrorHandlingMiddleware>();
        }

        public static IApplicationBuilder UseAuthenticationErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationErrorMiddleware>();
        }
    }
}
