using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace EVServiceCenter.Api.Middleware
{
    public class AuthenticationErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationErrorMiddleware> _logger;

        public AuthenticationErrorMiddleware(RequestDelegate next, ILogger<AuthenticationErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("JWT token expired for request: {Path}", context.Request.Path);
                await HandleTokenExpiredAsync(context);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("JWT token validation failed: {Error}", ex.Message);
                await HandleInvalidTokenAsync(context);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt to: {Path}", context.Request.Path);
                await HandleUnauthorizedAsync(context);
            }
        }

        private async Task HandleTokenExpiredAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại.",
                error = "TOKEN_EXPIRED",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private async Task HandleInvalidTokenAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Token không hợp lệ. Vui lòng đăng nhập lại.",
                error = "INVALID_TOKEN",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private async Task HandleUnauthorizedAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Bạn không có quyền truy cập tài nguyên này.",
                error = "FORBIDDEN",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
